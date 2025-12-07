using Humanizer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
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
                .Where(p => p.MotherPostId == null)
                .OrderByDescending(p => p.CreatedAt);

            ViewBag.Posts = posts;

            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            return View();
        }

        public IActionResult Show(int id)
        {
            Post? post = db.Posts
                .Include(p => p.User)
                .Include(p => p.Community)
                .Where(p => p.Id == id)
                .FirstOrDefault();
            
            if (post is null)
                return NotFound();

            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            return View(post);
        }

        [HttpGet]
        public IActionResult New()
        {
            Post post = new Post();

            post.AvailableCommunities = getAvailableCommunities();

            return View(post);
        }

        [HttpPost]
        public IActionResult New(Post post)
        {
            post.CreatedAt = DateTime.Now;
            post.Likes = 0;
            post.UserId = _userManager.GetUserId(User);

            Debug.WriteLine("HERE\n");

            if (post.Title is null)
            {
                ModelState.AddModelError("Title", "The Title Field is required!");
                post.AvailableCommunities = getAvailableCommunities();
                return View(post);
            }
            else
                ModelState.Remove("Title");

            if (ModelState.IsValid)
            {
                db.Posts.Add(post);
                db.SaveChanges();
                TempData["message"] = "New post created";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Index");
            }
            else
            {
                post.AvailableCommunities = getAvailableCommunities();
                return View(post);
            }
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
    }
}
