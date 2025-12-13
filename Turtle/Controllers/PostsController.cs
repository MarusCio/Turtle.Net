using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Turtle.Data;
using Turtle.Models;

namespace Turtle.Controllers
{
    public class PostsController: Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public PostsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IActionResult Index()
        {
            var posts = db.Posts
                .Include(p => p.User)
                .Include(p => p.Community)
                .Include(p => p.PostCategories)
                    .ThenInclude(pc => pc.Category)
                .Where(p => p.MotherPostId == null)
                .OrderByDescending(p => p.CreatedAt);

            ViewBag.Posts = posts;

            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            foreach (var post in posts) {
                post.Liked = IsPostLiked(post.Id);
            }

            SetAccessRights();

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
            newComment.CommunityId = post.CommunityId;
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
            return View(post);
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

        [HttpPost]
        [Authorize(Roles = "Admin,User")]
        public IActionResult ToggleLike(int Id)
        {
            Post? post = db.Posts.Where(p => p.Id == Id).FirstOrDefault();

            if (post is null)
                return NotFound();

            PostLike? post_like = db.PostLikes
                .Where(p => p.UserId == _userManager.GetUserId(User) && p.PostId == Id)
                .FirstOrDefault();

            bool isLike = post_like is null;

            if (post_like is null)
            {
                post_like = new PostLike();

                post_like.PostId = Id;
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

            Post? root = GetRootPost(Id);

            if (root is null) return NotFound();

            return RedirectToAction("Show", new { id = root.Id });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,User")]
        public IActionResult New([FromForm] PostForm postForm)
        {
            if (postForm.Title is null)
            {
                ModelState.AddModelError("Title", "The Title Field is required!");
                postForm.AvailableCommunities = getAvailableCommunities();
                postForm.AvailableCategories = getAvailableCategories();
                return View(postForm);
            }
            else
                ModelState.Remove("Title");

            Post post = new Post();
            post.Title = postForm.Title;
            post.Content = postForm.Content;
            post.CommunityId = postForm.CommunityId;
            post.CreatedAt = DateTime.Now;
            post.Likes = 0;
            post.UserId = _userManager.GetUserId(User);

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

                TempData["message"] = "New post created";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Index");
            }
            else
            {
                postForm.AvailableCommunities = getAvailableCommunities();
                postForm.AvailableCategories = getAvailableCategories();
                return View(post);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,User")]
        public IActionResult Delete(int id)
        {
            Post? post = db.Posts.Where(p => p.Id == id).FirstOrDefault();

            if (post is null)
                return NotFound();

            if (post.UserId == _userManager.GetUserId(User) ||
                User.IsInRole("Admin"))
            {
                DeleteComments(id);
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

            return RedirectToAction("Index");
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

            postForm.SelectedCategoryIds = [];
            foreach (var postCategory in post.PostCategories)
            {
                postForm.SelectedCategoryIds.Add((int) postCategory.CategoryId);
            }

            return View(postForm);
        }

        [HttpPost]
        public IActionResult Edit([FromForm] PostForm postForm)
        {
            Post? post = db.Posts.Find(postForm.EditetPostId);

            if (post is null)
                return NotFound();

            if (post.UserId != _userManager.GetUserId(User) && !User.IsInRole("Admin"))
            {
                TempData["message"] = "You do not have permission to edit this post!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", new { id = postForm.EditetPostId });
            }

            post.Title = postForm.Title;
            post.Content = postForm.Content;

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
                TempData["message"] = "You cannot edit the post!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", new { id = postForm.EditetPostId });
            }

            return RedirectToAction("Show", new { id = postForm.EditetPostId });
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
}
