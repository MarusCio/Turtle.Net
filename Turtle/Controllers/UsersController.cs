using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Turtle.Data;
using Turtle.Models;
using Turtle.Models.ViewModels;

namespace Turtle.Controllers
{
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public UsersController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Show(string id )
        {
            var user = _db.Users
                .Include(u => u.Posts)
                .FirstOrDefault(u => u.Id == id);

            if (user == null)
                return NotFound();

            var currentUserId = _userManager.GetUserId(User);

            var posts = _db.Posts
               .Include(p => p.User)
               .Include(p => p.Community)
               .Include(p => p.PostCategories)
                   .ThenInclude(pc => pc.Category)
               .Where(p => p.MotherPostId == null && p.UserId == user.Id)
               .OrderByDescending(p => p.CreatedAt);

            foreach (var post in posts)
                post.Liked = IsPostLiked(post.Id);

            var likedPostsIds = _db.PostLikes
                        .Where(l => l.UserId == user.Id)
                        .Select(l => l.PostId);
                       
            var likedPosts = _db.Posts
               .Include(p => p.User)
               .Include(p => p.Community)
               .Include(p => p.PostCategories)
                   .ThenInclude(pc => pc.Category)
               .Where(p => p.MotherPostId == null && likedPostsIds.Contains(p.Id))
               .OrderByDescending(p => p.CreatedAt);

            var communities = _db.UserCommunities
                .Include(uc => uc.Community)
                .Where(uc => uc.UserId == user.Id)
                .OrderBy(c => c.JoinedAt);

            var followers = _db.UserFollows
                    .Include(uf => uf.Follower)
                    .Where(uf => uf.FollowingId == user.Id)
                    .OrderBy(uf => uf.Follower.UserName)
                    .Select(uf => uf.Follower);
        
            var following = _db.UserFollows
                .Include(uf => uf.Following)
                .Where(uf => uf.FollowerId == user.Id)
                .OrderBy(uf => uf.Following.UserName)
                .Select(uf => uf.Following); 


            foreach (var post in likedPosts)
                post.Liked = IsPostLiked(post.Id);

            bool isFollowing = false;

            if (currentUserId != null && currentUserId != user.Id)
            {
                isFollowing = _db.UserFollows.Any(f =>
                    f.FollowerId == currentUserId &&
                    f.FollowingId == user.Id
                );
            }

            var vm = new UserProfileViewModel
            {
                User = user,
                Communities = communities.ToList(),
                IsCurrentUser = currentUserId == user.Id,
                IsFollowing = isFollowing
            };


            var search = "";

            if (Convert.ToString(HttpContext.Request.Query["search"]) != null)
            {
                search = Convert.ToString(HttpContext.Request.Query["search"]).Trim(); // eliminam spatiile libere 

                // Cautare in Posts (Title si Content)
                List<int> postIds = posts
                                        .Include(p => p.PostCategories)
                                         .ThenInclude(pc => pc.Category)
                                        .Where
                                        (
                                         p => p.Title.Contains(search) ||
                                              p.Content.Contains(search)
                                        ).Select(p => p.Id).ToList();

                for (int i = 0; i < postIds.Count(); i++)
                    postIds[i] = GetRootPost(postIds[i]).Id;

                List<int> categoryPostIds = _db.PostCategories
                                    .Include(pc => pc.Category)
                                    .Where(pc => pc.Category.CategoryName.Contains(search))
                                    .Select(pc => (int)pc.PostId).ToList();

                postIds = postIds.Union(categoryPostIds).ToList();

                likedPosts = likedPosts.Where(posts => postIds.Contains(posts.Id))
                                      .Include(p => p.User)
                                      .Include(p => p.Community)
                                      .Include(p => p.PostCategories)
                                         .ThenInclude(pc => pc.Category)
                                      .OrderByDescending(p => p.Likes)
                                      .OrderByDescending(p => p.CreatedAt);
                                      

                posts = posts.Where(posts => postIds.Contains(posts.Id))
                                      .Include(p => p.User)
                                      .Include(p => p.Community)
                                      .Include(p => p.PostCategories)
                                         .ThenInclude(pc => pc.Category)
                                      .OrderByDescending(p => p.CreatedAt)
                                      .OrderByDescending(p => p.Likes);


                // Community Searching, doar prin numele comunitatii

                communities = communities
                        .Include(uc => uc.Community)
                        .Where(uc => uc.Community.CommunityName.Contains(search))
                        .OrderBy(c => c.JoinedAt);

                // Followers following searching

                followers = followers
                        .Where(f => f.UserName.Contains(search));
                following = following
                        .Where(f => f.UserName.Contains(search));
            }

            ViewBag.SearchString = search;

            vm.Posts = posts.ToList();
            vm.LikedPosts = likedPosts.ToList();
            vm.Communities = communities.ToList();
            vm.Following = following.ToList();
            vm.Followers = followers.ToList();

            foreach (var post in vm.Posts)
                post.Liked = IsPostLiked(post.Id);
            foreach (var post in vm.LikedPosts)
                post.Liked = IsPostLiked(post.Id);


            SetAccessRights();

            int _perPage = 3;
            // For Hottest Posts //
            int totalItems = posts.Count();
            var currentHotPage = Convert.ToInt32(HttpContext.Request.Query["postPage"]);
            var offset = 0;

            if (!currentHotPage.Equals(0))
                offset = (currentHotPage - 1) * _perPage;

            var paginatedPosts = posts.Skip(offset).Take(_perPage).ToList();

            ViewBag.lastPostPage = Math.Ceiling((float)totalItems / (float)_perPage);
            ViewBag.Posts = paginatedPosts;
            ViewBag.PostPage = currentHotPage;
            // For Hottest Posts //

            // For Recent Posts //
            totalItems = likedPosts.Count();
            var currentRecPage = Convert.ToInt32(HttpContext.Request.Query["likedPostPage"]);
            offset = 0;

            if (!currentRecPage.Equals(0))
                offset = (currentRecPage - 1) * _perPage;

            paginatedPosts = likedPosts.Skip(offset).Take(_perPage).ToList();

            ViewBag.lastLikedPostPage = Math.Ceiling((float)totalItems / (float)_perPage);
            ViewBag.LikedPosts = paginatedPosts;
            ViewBag.LikedPostPage = currentRecPage;
            // For Recent Posts //

            var tab = HttpContext.Request.Query["tab"].ToString();
            if (string.IsNullOrEmpty(tab)) tab = "posts";

            ViewBag.Tab = tab;

            if (search != "")
            {
                ViewBag.PaginationBaseUrl = "/Users/Show/" + user.Id + "/?search=" + search + "&";
            }
            else
            {
                ViewBag.PaginationBaseUrl = "/Users/Show/" + user.Id + "/?";
            }

            return View(vm);
        }

        [Authorize(Roles ="Admin,User")]
        [HttpPost]
        public async Task<IActionResult> ToggleFollow(string id)
        {
            var currentUserId = _userManager.GetUserId(User);

            if (currentUserId == id)
                return BadRequest();

            var follow = await _db.UserFollows.FirstOrDefaultAsync(f =>
                f.FollowerId == currentUserId &&
                f.FollowingId == id
            );

            if (follow == null)
            {
                _db.UserFollows.Add(new UserFollow
                {
                    FollowerId = currentUserId,
                    FollowingId = id
                });
            }
            else
            {
                _db.UserFollows.Remove(follow);
            }

            await _db.SaveChangesAsync();

            return RedirectToAction("Show", new { id });
        }

        [Authorize(Roles ="Admin,User")]
        [HttpGet]
        public IActionResult Edit(string id)
        {
            var currentUserId = _userManager.GetUserId(User);

            // doar user-ul care a creat
            if (currentUserId != id)
                return Forbid();

            var user = _db.Users.Find(id);

            if (user == null)
                return NotFound();

            return View(user);
        }

        [Authorize(Roles ="Admin,User")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ApplicationUser model, IFormFile? profileImage)
        {
            var currentUserId = _userManager.GetUserId(User);

            // doar user-ul care 
            if (currentUserId != model.Id)
                return Forbid();

            var user = await _db.Users.FindAsync(model.Id);
            if (user == null)
                return NotFound();

            if (ModelState.IsValid)
            {
                // Update username and description
                user.UserName = model.UserName;
                user.Description = model.Description;

                // Handle profile image upload
                if (profileImage != null && profileImage.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/profiles");
                    
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(profileImage.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await profileImage.CopyToAsync(stream);
                    }

                    var oldfilePath = Path.Combine(
                                Directory.GetCurrentDirectory(), 
                                "wwwroot", 
                                user.ProfileImageUrl.TrimStart('/'));
                    if (user.ProfileImageUrl != null && System.IO.File.Exists(oldfilePath))
                    {
                        System.IO.File.Delete(oldfilePath);
                    }

                    user.ProfileImageUrl = $"/images/profiles/{fileName}";
                }

                _db.Users.Update(user);
                await _db.SaveChangesAsync();

                return RedirectToAction("Show", new { id = user.Id });
            }

            return View(model);
        }


    [NonAction]
        private void SetAccessRights()
        {
            ViewBag.CurrentUserId = _userManager.GetUserId(User);
            ViewBag.UserIsAdmin = User.IsInRole("Admin");
        }

        [NonAction]
        private Post? GetRootPost(int id)
        {
            Post? post = _db.Posts.Find(id);

            while (post is not null && post.MotherPostId is not null)
            {
                post = _db.Posts.Find(post.MotherPostId);
            }

            return post;
        }

        [NonAction]
        private bool IsPostLiked(int Id)
        {
            PostLike? post_like = _db.PostLikes
                .Where(p => p.PostId == Id && p.UserId == _userManager.GetUserId(User))
                .FirstOrDefault();

            if (post_like == null)
                return false;
            return true;
        }
    }
}
