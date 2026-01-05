//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using Turtle.Data;
//using Turtle.Models;


//namespace Turtle.Controllers
//{
//    public class UsersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : Controller
//    {
//        private readonly ApplicationDbContext db = context;
//        private readonly UserManager<ApplicationUser> _userManager = userManager;



//        [HttpGet]
//        public IActionResult Profile(string id)
//        {
//            if (string.IsNullOrEmpty(id))
//                return NotFound();

//            var user = db.Users
//                .Include(u => u.Posts)
//                .Include(u => u.UserCommunities)
//                    .ThenInclude(uc => uc.Community)
//                .Include(u => u.Followers)
//                    .ThenInclude(f => f.Follower)
//                .Include(u => u.Following)
//                    .ThenInclude(f => f.Following)
//                .FirstOrDefault(u => u.Id == id);

//            if (user == null)
//                return NotFound();

//            string? currentUserId = _userManager.GetUserId(User);

//            ViewBag.CurrentUserId = currentUserId;
//            ViewBag.IsOwner = currentUserId == user.Id;

//            // verificam daca userul curent urmareste acest profil
//            ViewBag.IsFollowing = false;

//            if (User.Identity != null && User.Identity.IsAuthenticated && currentUserId != null)
//            {
//                ViewBag.IsFollowing = db.UserFollows.Any(f =>
//                    f.FollowerId == currentUserId &&
//                    f.FollowingId == user.Id
//                );
//            }

//            return View(user);
//        }


//        [Authorize]
//        [HttpPost]
//        public IActionResult Follow(string userId)
//        {
//            var currentUserId = _userManager.GetUserId(User);

//            if (string.IsNullOrEmpty(userId) || currentUserId == null)
//                return BadRequest();

//            // nu te poti urmari pe tine
//            if (currentUserId == userId)
//                return BadRequest();

//            bool alreadyFollowing = db.UserFollows.Any(f =>
//                f.FollowerId == currentUserId &&
//                f.FollowingId == userId
//            );

//            if (!alreadyFollowing)
//            {
//                var follow = new UserFollow
//                {
//                    FollowerId = currentUserId,
//                    FollowingId = userId,
//                    FollowedAt = DateTime.Now
//                };

//                db.UserFollows.Add(follow);
//                db.SaveChanges();
//            }

//            return RedirectToAction("Profile", new { id = userId });
//        }

//        // ============================
//        // UNFOLLOW
//        // ============================
//        [Authorize]
//        [HttpPost]
//        public IActionResult Unfollow(string userId)
//        {
//            var currentUserId = _userManager.GetUserId(User);

//            if (string.IsNullOrEmpty(userId) || currentUserId == null)
//                return BadRequest();

//            var follow = db.UserFollows.FirstOrDefault(f =>
//                f.FollowerId == currentUserId &&
//                f.FollowingId == userId
//            );

//            if (follow != null)
//            {
//                db.UserFollows.Remove(follow);
//                db.SaveChanges();
//            }

//            return RedirectToAction("Profile", new { id = userId });
//        }

//        // ============================
//        // LISTA FOLLOWERS (OPTIONAL)
//        // ============================
//        [HttpGet]
//        public IActionResult Followers(string id)
//        {
//            var user = db.Users
//                .Include(u => u.Followers)
//                    .ThenInclude(f => f.Follower)
//                .FirstOrDefault(u => u.Id == id);

//            if (user == null)
//                return NotFound();

//            return View(user);
//        }

//        // ============================
//        // LISTA FOLLOWING (OPTIONAL)
//        // ============================
//        [HttpGet]
//        public IActionResult Following(string id)
//        {
//            var user = db.Users
//                .Include(u => u.Following)
//                    .ThenInclude(f => f.Following)
//                .FirstOrDefault(u => u.Id == id);

//            if (user == null)
//                return NotFound();

//            return View(user);
//        }
//    }
//}