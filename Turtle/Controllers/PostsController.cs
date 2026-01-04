using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Build.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol.Plugins;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Turtle.Data;
using Turtle.Models;
using Turtle.Services;


namespace Turtle.Controllers
{
    public class PostsController: Controller
    {
        private readonly ApplicationDbContext db;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly GoogleCategoryAnalysisService _categoryService;
        private readonly long MaxBSize = 20_000_000;

        public PostsController(
                ApplicationDbContext context, 
                IWebHostEnvironment env, 
                UserManager<ApplicationUser> userManager, 
                RoleManager<IdentityRole> roleManager,
                GoogleCategoryAnalysisService categoryService)
        {
            db = context;
            _env = env;
            _userManager = userManager;
            _roleManager = roleManager;
            _categoryService = categoryService;
        }

        public IActionResult Index()
        {
            var hot_posts = db.Posts
                .Include(p => p.User)
                .Include(p => p.Community)
                .Include(p => p.PostCategories)
                    .ThenInclude(pc => pc.Category)
                .Where(p => p.MotherPostId == null)
                .OrderByDescending(p => p.CreatedAt)
                .OrderByDescending(p => p.Likes);
            var recent_posts = db.Posts
                .Include(p => p.User)
                .Include(p => p.Community)
                .Include(p => p.PostCategories)
                    .ThenInclude(pc => pc.Category)
                .Where(p => p.MotherPostId == null)
                .OrderByDescending(p => p.Likes)
                .OrderByDescending(p => p.CreatedAt);
                

            ViewBag.HottestPosts = hot_posts;
            ViewBag.RecentPosts = recent_posts;

            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            var search = "";

            if (Convert.ToString(HttpContext.Request.Query["search"]) != null)
            {
                search = Convert.ToString(HttpContext.Request.Query["search"]).Trim(); // eliminam spatiile libere 

                // Cautare in Posts (Title si Content)
                List<int> postIds = db.Posts
                                        .Include(p => p.PostCategories)
                                         .ThenInclude(pc => pc.Category)
                                        .Where
                                        (
                                         p => p.Title.Contains(search) ||
                                              p.Content.Contains(search)
                                        ).Select(p => p.Id).ToList();

                for (int i = 0; i < postIds.Count(); i++)
                    postIds[i] = GetRootPost(postIds[i]).Id;

                List<int> categoryPostIds = db.PostCategories
                                    .Include(pc => pc.Category)
                                    .Where(pc => pc.Category.CategoryName.Contains(search))                                    
                                    .Select(pc => (int) pc.PostId).ToList();

                postIds = postIds.Union(categoryPostIds).ToList();
                    
                hot_posts = db.Posts.Where(posts => postIds.Contains(posts.Id))
                                      .Include(p => p.User)
                                      .Include(p => p.Community)
                                      .Include(p => p.PostCategories)
                                         .ThenInclude(pc => pc.Category)
                                      .OrderByDescending(p => p.CreatedAt)
                                      .OrderByDescending(p => p.Likes);

                recent_posts = db.Posts.Where(posts => postIds.Contains(posts.Id))
                                      .Include(p => p.User)
                                      .Include(p => p.Community)
                                      .Include(p => p.PostCategories)
                                         .ThenInclude(pc => pc.Category)
                                      .OrderByDescending(p => p.CreatedAt)
                                      .OrderByDescending(p => p.Likes);
            }

            ViewBag.SearchString = search;

            foreach (var post in hot_posts)      
                post.Liked = IsPostLiked(post.Id);
            foreach (var post in recent_posts)
                post.Liked = IsPostLiked(post.Id);


            SetAccessRights();

            int _perPage = 3;
            // For Hottest Posts //
            int totalItems = hot_posts.Count();
            var currentHotPage = Convert.ToInt32(HttpContext.Request.Query["pageH"]);
            var offset = 0;

            if (!currentHotPage.Equals(0)) 
                offset = (currentHotPage - 1) * _perPage;
            
            var paginatedPosts = hot_posts.Skip(offset).Take(_perPage).ToList();

            ViewBag.lastHottestPage = Math.Ceiling((float)totalItems / (float) _perPage);
            ViewBag.HottestPosts = paginatedPosts;
            ViewBag.HotPage = currentHotPage;
            // For Hottest Posts //

            // For Recent Posts //
            totalItems = recent_posts.Count();
            var currentRecPage = Convert.ToInt32(HttpContext.Request.Query["pageR"]);
            offset = 0;

            if (!currentRecPage.Equals(0))
                offset = (currentRecPage - 1) * _perPage;

            paginatedPosts = recent_posts.Skip(offset).Take(_perPage).ToList();

            ViewBag.lastRecentPost = Math.Ceiling((float)totalItems / (float)_perPage);
            ViewBag.RecentPosts = paginatedPosts;
            ViewBag.RecPage = currentRecPage;
            // For Recent Posts //

            var tab = HttpContext.Request.Query["tab"].ToString();
            if (string.IsNullOrEmpty(tab)) tab = "hot";
            ViewBag.Tab = tab;

            if (search != "")
            {
                ViewBag.PaginationBaseUrl = "/Posts/Index/?search=" + search + "&";
            }
            else
            {
                ViewBag.PaginationBaseUrl = "/Posts/Index/?";
            }

            return View();
        }

        [Authorize(Roles = "Admin,User")]
        public IActionResult Show(int id)
        {

            Post? post = db.Posts.Where(p => p.Id == id).FirstOrDefault();
            while (post != null && post.MotherPostId != null)
            {

                id = (int)post.MotherPostId;
                post = db.Posts.Where(p => p.Id == id).FirstOrDefault();
            }

            post = LoadPostTree(id);

            if (post is null)
                return NotFound();

            post.Liked = IsPostLiked(id);

            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            SetAccessRights();

            return View(post);
        }
        [HttpPost]
        [Authorize(Roles = "Admin,User")]
        public IActionResult Show([FromForm] CommentForm comment) {
            Post post = db.Posts.Find(comment.MotherPostId);

            if (post is null) return NotFound();

            while (post.MotherPostId is not null)
            {
                post = db.Posts.Find(post.MotherPostId);
            }

            if (post is null) return NotFound();

            Post newComment = new Post();
            newComment.UserId = _userManager.GetUserId(User);
            newComment.CreatedAt = DateTime.Now;
            newComment.CommunityId = null;
            newComment.Likes = 0;
            newComment.Title = null;
            newComment.Content = comment.Content;
            newComment.MotherPostId = comment.MotherPostId;

            if (ModelState.IsValid)
            {
                db.Posts.Add(newComment);
                db.SaveChanges();

                post = LoadPostTree(post.Id);
                post.Liked = IsPostLiked(post.Id);

                ViewBag.Message = "New comment added!";
                ViewBag.Alert = "alert-success";
            }
            else
            {
                post = LoadPostTree(post.Id);
                post.Liked = IsPostLiked(post.Id);
            }

            SetAccessRights();
            return RedirectToAction("Show", new {id = post.Id});
        }

        [HttpGet]
        [Authorize(Roles = "Admin,User")]
        public IActionResult New()
        {
            PostForm post = new PostForm();

            post.AvailableCommunities = getAvailableCommunities();
            post.AvailableCategories = getAvailableCategories();

            return View(post);
        }

        //[HttpPost]
        //[Authorize(Roles = "Admin,User")]
        //public async Task<IActionResult> ToggleLike(int Id)
        //{
        //    Post? post = db.Posts.Where(p => p.Id == Id).FirstOrDefault();

        //    if (post is null)
        //        return NotFound();

        //    PostLike? post_like = db.PostLikes
        //        .Where(p => p.UserId == _userManager.GetUserId(User) && p.PostId == Id)
        //        .FirstOrDefault();

        //    bool isLike = post_like is null;

        //    if (post_like is null)
        //    {
        //        post_like = new PostLike();

        //        post_like.PostId = Id;
        //        post_like.UserId = _userManager.GetUserId(User);
        //        post_like.LikedAt = DateTime.Now;

        //        post.Likes++;

        //        if (ModelState.IsValid)
        //        {
        //            db.PostLikes.Add(post_like);
        //            db.Posts.Update(post);
        //            db.SaveChanges();
        //        }
        //        else
        //        {
        //            TempData["message"] = "Cannot like current post!";
        //            TempData["messageType"] = "alert-danger";
        //        }
        //    }
        //    else
        //    {
        //        post.Likes--;

        //        if (ModelState.IsValid)
        //        {
        //            db.PostLikes.Remove(post_like);
        //            db.Posts.Update(post);
        //            db.SaveChanges();
        //        }
        //        else
        //        {

        //            TempData["message"] = "Cannot unlike current post!";
        //            TempData["messageType"] = "alert-danger";
        //        }
        //    }

        //    Post? root = GetRootPost(Id);

        //    if (root is null) return NotFound();

        //    return RedirectToAction("Show", new { id = root.Id });
        //}

        

        [HttpPost]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> ToggleLike([FromBody] LikeRequest lr)
        {
            int id = lr.id;
            for (int i = 0; i < 5; i++) Console.WriteLine("<-------->");
            Console.WriteLine(id);
            for (int i = 0; i < 5; i++) Console.WriteLine("<-------->");
            Post? post = db.Posts.Where(p => p.Id == id).FirstOrDefault();

            if (post is null)
                return BadRequest("No post to like");

            PostLike? post_like = db.PostLikes
                .Where(p => p.UserId == _userManager.GetUserId(User) && p.PostId == id)
                .FirstOrDefault();

            bool isLike = post_like is null;

            if (post_like is null)
            {
                post_like = new PostLike();

                post_like.PostId = id;
                post_like.UserId = _userManager.GetUserId(User);
                post_like.LikedAt = DateTime.Now;

                post.Likes++;

                if (ModelState.IsValid)
                {
                    db.PostLikes.Add(post_like);
                    db.Posts.Update(post);
                    db.SaveChanges();
                }
                else
                {
                    TempData["message"] = "Cannot like current post!";
                    TempData["messageType"] = "alert-danger";
                }
            }
            else
            {
                post.Likes--;

                if (ModelState.IsValid)
                {
                    db.PostLikes.Remove(post_like);
                    db.Posts.Update(post);
                    db.SaveChanges();
                }
                else
                {

                    TempData["message"] = "Cannot unlike current post!";
                    TempData["messageType"] = "alert-danger";
                }
            }

            Post? root = GetRootPost(id);

            if (root is null) return BadRequest("No root post");

            return Json(new { success = true, likes = post.Likes }); 
        }


        [HttpPost]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> New(PostForm postForm, List<IFormFile> Files)
        {
            CategoriesResult categoryResult = await _categoryService.AnalyzeCategoriesAsync(postForm.Title, postForm.Content);

            if (categoryResult != null && categoryResult.Success)
            {
                for (int i = 0; i < 5; i++) Console.WriteLine("<----------->");
                foreach (string categorie in categoryResult.SuggestedCategoriesNames)
                    Console.WriteLine(categorie);
                for (int i = 0; i < 5; i++) Console.WriteLine("<----------->");
            }

            postForm.Files = [];

            Post post = new Post();
            post.Title = postForm.Title;
            post.Content = postForm.Content;
            post.CommunityId = postForm.CommunityId;
            post.CreatedAt = DateTime.Now;
            post.Likes = 0;
            post.UserId = _userManager.GetUserId(User);
            post.Files = [];

            if (ModelState.IsValid)
            {
                db.Posts.Add(post);
                db.SaveChanges();

                foreach (var cat in postForm.SelectedCategoryIds)
                {
                    PostCategory pc = new PostCategory();
                    pc.PostId = post.Id;
                    pc.CategoryId = cat;
                    db.PostCategories.Add(pc);
                }
                db.SaveChanges();
            }
            else
            {
                postForm.AvailableCommunities = getAvailableCommunities();
                postForm.AvailableCategories = getAvailableCategories();
                return View(postForm);
            }


            if (Files != null && Files.Count() > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".png", ".jpeg", ".pdf", ".txt", ".c", ".cpp", ".py", ".cs" };
                postForm.Files = [];
                long sum = 0;
                foreach (IFormFile file in Files)
                {
                    sum += file.Length;
                }
                
                if (sum > MaxBSize)
                {
                    ModelState.AddModelError("Files", "Files must have a combined maximum of 20 MB!");
                    
                    DeleteFiles(post.Id, postForm.Files);
                    db.Posts.Remove(post);
                    db.SaveChanges();
                    
                    postForm.AvailableCommunities = getAvailableCommunities();
                    postForm.AvailableCategories = getAvailableCategories();
                    return View(postForm);
                }

                foreach (IFormFile file in Files)
                {
                    if (file.Length > 0)
                    {
                        var fileExtension = Path.GetExtension(file.FileName).ToLower();

                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            ModelState.AddModelError("Files", "Files must be a of the following extensions: (jpg, jpeg, png, pdf, txt, c, cpp, py, cs");

                            DeleteFiles(post.Id, postForm.Files);
                            db.Posts.Remove(post);
                            db.SaveChanges();
                            
                            postForm.AvailableCommunities = getAvailableCommunities();
                            postForm.AvailableCategories = getAvailableCategories();   
                            return View(postForm);
                        }

                        //Cale Stocare
                        var storagePath = Path.Combine(_env.WebRootPath, "files", "posts", file.FileName + post.Id);
                        var databaseFileName = "/files/posts/" + file.FileName;

                        //Salvare fisiere
                        using (var filestream = new FileStream(storagePath, FileMode.Create))
                        {
                            await file.CopyToAsync(filestream);
                        }

                        postForm.Files.Add(databaseFileName);
                    }
                }

                ModelState.Remove(nameof(postForm.Files));
            }

            if (postForm.Title is null)
            {
                ModelState.AddModelError("Title", "The Title Field is required!");
                postForm.AvailableCommunities = getAvailableCommunities();
                postForm.AvailableCategories = getAvailableCategories();
                return View(postForm);
            }
            else
                ModelState.Remove("Title");


            post.Files = postForm.Files;
            db.Posts.Update(post);
            db.SaveChanges();

            TempData["message"] = "New post created";
            TempData["messageType"] = "alert-success";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = "Admin,User")]
        public IActionResult Delete(int id, string? search, string? tab, string? pageH, string? pageR)
        {
            Post? post = db.Posts.Where(p => p.Id == id).FirstOrDefault();

            if (post is null)
                return NotFound();

            if (post.UserId == _userManager.GetUserId(User) ||
                User.IsInRole("Admin"))
            {
                DeleteComments(id);
                DeleteFiles(post.Id, post.Files);

                var postLikes = db.PostLikes.Where(x => x.PostId == id);
                db.RemoveRange(postLikes);
                db.Posts.Remove(post);
                db.SaveChanges();

                TempData["message"] = "Post has been removed!";
                TempData["messageType"] = "alert-success";
            }
            else
            {
                TempData["message"] = "You do not have the right to delete the post!";
                TempData["messageType"] = "alert-danger";
            }



            return RedirectToAction("Index", new
            {
                search, tab, pageH, pageR
            });
        }

        [HttpGet]
        [Authorize(Roles = "Admin,User")]
        public IActionResult Edit(int id)
        {
            Post? post = db.Posts
                .Include(p => p.PostCategories)
                .Where(p => p.Id == id)
                .FirstOrDefault();

            if (post is null) return NotFound();

            if (post.UserId != _userManager.GetUserId(User) && !User.IsInRole("Admin"))
            {
                TempData["message"] = "You do not have permission to edit this post!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", new { id = id });
            }

            PostForm postForm = new PostForm();
            postForm.Title = post.Title;
            postForm.Content = post.Content;
            postForm.AvailableCommunities = getAvailableCommunities();
            postForm.AvailableCategories = getAvailableCategories();
            postForm.IsRootPost = post.MotherPostId == null;
            postForm.EditetPostId = post.Id;
            postForm.ExistingFiles = post.Files;
            postForm.PostId = post.Id;

            postForm.SelectedCategoryIds = [];
            foreach (var postCategory in post.PostCategories)
            {
                postForm.SelectedCategoryIds.Add((int) postCategory.CategoryId);
            }

            return View(postForm);
        }

        [HttpPost]
        public async Task<IActionResult> Edit([FromForm] PostForm postForm, List<IFormFile> Files)
        {
            postForm.Files = [];

            Post? post = db.Posts
                 .Include(p => p.PostCategories)
                 .Where(p => p.Id == postForm.EditetPostId)
                 .FirstOrDefault();

            if (post is null)
                return NotFound();

            if (post.UserId != _userManager.GetUserId(User) && !User.IsInRole("Admin"))
            {
                TempData["message"] = "You do not have permission to edit this post!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", new { id = postForm.EditetPostId });
            }

            if (ModelState.IsValid)
            {
                DeleteFiles(post.Id, postForm.FilesToDelete);
                
                foreach (string f in postForm.FilesToDelete)
                {  
                    post.Files.Remove("/files/posts/" + f);
                }

                db.Posts.Update(post);
                db.SaveChanges();
            }

            post.Title = postForm.Title;
            post.Content = postForm.Content;

            if (Files.Count() > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".png", ".jpeg", ".pdf", ".txt", ".c", ".cpp", ".py", ".cs" };
                postForm.Files = [];
                long sum = 0;
                foreach (IFormFile file in Files)
                {
                    sum += file.Length;
                }

                if (sum > MaxBSize)
                {
                    ModelState.AddModelError("Files", "Files must have a combined maximum of 20 MB!");

                    DeleteFiles(post.Id, postForm.Files);

                    //postForm.AvailableCommunities = getAvailableCommunities();
                    //postForm.AvailableCategories = getAvailableCategories();
                    return View(postForm);
                }

                foreach (IFormFile file in Files)
                {
                    if (file.Length > 0)
                    {
                        var fileExtension = Path.GetExtension(file.FileName).ToLower();

                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            ModelState.AddModelError("Files", "Files must be a of the following extensions: (jpg, jpeg, png, pdf, txt, c, cpp, py, cs");

                            DeleteFiles(post.Id, postForm.Files);

                            //postForm.AvailableCommunities = getAvailableCommunities();
                            //postForm.AvailableCategories = getAvailableCategories();
                            return View(postForm);
                        }

                        //Cale Stocare
                        var storagePath = Path.Combine(_env.WebRootPath, "files", "posts", file.FileName + post.Id);
                        var databaseFileName = "/files/posts/" + file.FileName;

                        //Salvare fisiere
                        using (var filestream = new FileStream(storagePath, FileMode.Create))
                        {
                            await file.CopyToAsync(filestream);
                        }

                        postForm.Files.Add(databaseFileName);
                    }
                }

                ModelState.Remove(nameof(postForm.Files));
            }

            post.Files.AddRange(postForm.Files);

            if (ModelState.IsValid)
            {
                var postCategories = db.PostCategories
                    .Where(p => p.PostId == post.Id);

                db.PostCategories.RemoveRange(postCategories);

                foreach (var categoryId in postForm.SelectedCategoryIds)
                {
                    PostCategory pc = new PostCategory();
                    pc.PostId = post.Id;
                    pc.CategoryId = categoryId;

                    db.PostCategories.Add(pc);
                }

                db.Update(post);
                db.SaveChanges();

                TempData["message"] = "Post Edited!";
                TempData["messageType"] = "alert-success";
            }
            else
            {
                return View(postForm);
            }

            return RedirectToAction("Show", new { id = postForm.EditetPostId });
        }

        [HttpPost]
        public async Task<IActionResult> SuggestCategories([FromBody] PostForm model)
        {
            var result = await _categoryService.AnalyzeCategoriesAsync(model.Title, model.Content);

            if (!result.Success)
                return BadRequest(result.ErrorMessage);

            return Json(new { categories = result.SuggestedCategoriesNames });
        }


        [NonAction]
        private bool IsPostLiked(int Id)
        {
            PostLike? post_like = db.PostLikes
                .Where(p => p.PostId == Id && p.UserId == _userManager.GetUserId(User))
                .FirstOrDefault();

            if (post_like == null)
                return false;
            return true;
        }
        [NonAction]
        private IEnumerable<SelectListItem> getAvailableCommunities()
        {
            // generam o lista de tipul SelectListItem fara elemente
            var selectList = new List<SelectListItem>();

            var communitiesIds = db.UserCommunities
                .Where(p => p.UserId == _userManager.GetUserId(User))
                .Select(p => p.CommunityId);

            var communities = db.Communities
                .Where(com => communitiesIds.Contains(com.Id));

            // iteram prin categorii
            foreach (var community in communities)
            {
                // adaugam in lista elementele necesare pentru dropdown
                // id-ul categoriei si denumirea acesteia
                selectList.Add(new SelectListItem
                {
                    Value = community.Id.ToString(),
                    Text = community.CommunityName
                });
            }

            return selectList;
        }
        [NonAction]
        private IEnumerable<SelectListItem> getAvailableCategories()
        {
            var selectList = new List<SelectListItem>();

            var categories = from cat in db.Categories
                             select cat;

            foreach (var category in categories)
            {
                selectList.Add(new SelectListItem
                {
                    Value = category.Id.ToString(),
                    Text = category.CategoryName
                });
            }

            return selectList;
        }
        [NonAction]
        private Post? LoadPostTree (int id)
        {
            Post? post = db.Posts
                .Include(p => p.User)
                .Include(p => p.Community)
                .Include(p => p.PostCategories)
                    .ThenInclude(pc => pc.Category)
                .Where(p => p.Id == id)
                .FirstOrDefault();

            LoadComments(post);

            return post;
        }

        [NonAction]
        private void DeleteFiles(int id, List<string> Files)
        {
            foreach (string file in Files) {
                var storagePath = Path.Combine(_env.WebRootPath, "files", "posts", Path.GetFileName(file) + id.ToString());

                if (System.IO.File.Exists(storagePath))
                {
                    System.IO.File.Delete(storagePath);
                }
            }
        }
        [NonAction]
        private void LoadComments(Post? post)
        {
            if (post is null)
                return;

            var comments = db.Posts
                .Include(p => p.User)
                .Where(p => p.MotherPostId == post.Id)
                .OrderByDescending(p => p.Likes)
                .ToList();

            post.Comments = comments;

            foreach (var comment in comments)
            {
                comment.Liked = IsPostLiked(comment.Id);
                LoadComments(comment);
            }
        }
        [NonAction]
        private Post? GetRootPost (int id)
        {
            Post? post = db.Posts.Find(id);

            while (post is not null && post.MotherPostId is not null)
            {
                post = db.Posts.Find(post.MotherPostId);
            }

            return post;
        }
        [NonAction]
        private void DeleteComments(int id)
        {
            var comments = db.Posts.Where(p => p.MotherPostId == id);

            foreach (var comment in comments)
            {
                DeleteComments(comment.Id);
                var postlikes = db.PostLikes.Where(x => x.PostId == comment.Id);
                db.PostLikes.RemoveRange(postlikes);
                db.Posts.Remove(comment);
            }
        }
        [NonAction]
        private void SetAccessRights()
        {
            ViewBag.CurrentUserId = _userManager.GetUserId(User);
            ViewBag.UserIsAdmin = User.IsInRole("Admin");
        }
    }

    public class LikeRequest
    {
        public int id { get; set; }
    }
}
