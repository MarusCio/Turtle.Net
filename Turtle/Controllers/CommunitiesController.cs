using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Turtle.Data;
using Turtle.Models;


namespace Turtle.Controllers
{
    public class CommunitiesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager) : Controller
    {

        private readonly ApplicationDbContext db = context;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;

        public IActionResult Index()
        {
            var communities = db.Communities
                             .Include(c => c.Creator)
                             .OrderByDescending(c => c.CreatedAt);

            ViewBag.Communities = communities;

            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];

            }

            return View();
        }


        public IActionResult Show(int id)
        {
            Community? community = db.Communities
                                 .Include(c => c.Creator) //userul care a creat comunitatea
                                 .Include(c => c.UserCommunities) //membrii comunitatii
                                    .ThenInclude(uc => uc.User) //userii membri
                                 .Include(c => c.PostsCommunity) //postarile din comunitate
                                    .ThenInclude(p => p.User) //userii care au scris postarile
                                 .Where(c => c.Id == id)
                                 .FirstOrDefault();

            if (community is null)
            {
                return NotFound();
            }

            //SetAccessRights();


            // verificare daca userul curent este membru al comunitatii
            var user = _userManager.GetUserAsync(User).Result;

            ViewBag.IsMember = false;        
            ViewBag.CurrentRole = null;
            ViewBag.CurrentUserId = _userManager.GetUserId(User); 

            if (user != null)
            {
                // gasim membership-ul userului in comunitate
                var membership = db.UserCommunities
                    .FirstOrDefault(uc => uc.CommunityId == community.Id && uc.UserId == user.Id);

                if (membership != null)
                {
                    ViewBag.IsMember = true;
                    ViewBag.CurrentRole = membership.Role;
                }
            }

            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            return View(community);
        }


        [Authorize(Roles = "User,Admin")]
        public IActionResult New()
        {
            Community community = new Community();
            return View(community);
        }

        // Se adauga comunitatea in baza de date
        [HttpPost]
        [Authorize(Roles = "User,Admin")] //user logat, admin
        public IActionResult New(Community community)
        {
            community.CreatedAt = DateTime.Now;

            //preluam creatorul comunitatii
            string CreatorId = _userManager.GetUserId(User);
            community.CreatorId = CreatorId;

            if (ModelState.IsValid)
            {
                db.Communities.Add(community);
                db.SaveChanges();

                // Adaugam creatorul ca membru al comunitatii
                var CommunityCreator = new UserCommunity()
                {
                    UserId = CreatorId,
                    CommunityId = community.Id,
                    JoinedAt = DateTime.Now,
                    Role = CommunityRole.Admin

                };

                db.UserCommunities.Add(CommunityCreator);
                db.SaveChanges();


                TempData["message"] = "Comunitatea a fost adaugata";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Index");
            }

            else
            {
                return View(community);
            }
        }



        [Authorize(Roles = "User,Admin")]
        public IActionResult ChangeRole(int userCommunityId, CommunityRole newRole)
        {
            var UserCommunity = db.UserCommunities
                                .Include(uc => uc.Community)
                                .FirstOrDefault(uc => uc.Id == userCommunityId);

            if (UserCommunity == null)
            {
                return NotFound();
            }

            //verificam daca userul curent are dreptul sa schimbe rolul altui utilizator  (admin sau moderator comunitate)
            var CurrentUser = _userManager.GetUserAsync(User).Result;

            bool CurrentUserAdmin = db.UserCommunities
                                        .Any(uc => uc.CommunityId == UserCommunity.CommunityId &&
                                             uc.UserId == CurrentUser.Id &&
                                             uc.Role == CommunityRole.Admin);

            bool CurrentUserModerator = db.UserCommunities
                                            .Any(uc => uc.CommunityId == UserCommunity.CommunityId &&
                                                 uc.UserId == CurrentUser.Id &&
                                                 uc.Role == CommunityRole.Moderator);

            if (UserCommunity.Role == CommunityRole.Admin && !CurrentUserAdmin)
            {
                TempData["message"] = "Nu aveti dreptul sa schimbati rolul unui administrator.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", new { id = UserCommunity.CommunityId });
            }

            if (CurrentUserModerator && newRole == CommunityRole.Admin)
            {
                TempData["message"] = "Doar adminii pot face pe altcineva admin";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", new { id = UserCommunity.CommunityId });
            }


            // se permite schimbarea rolului
            UserCommunity.Role = newRole;
            db.SaveChanges();

            TempData["message"] = "Role updated successfully!";
            TempData["messageType"] = "alert-success";

            return RedirectToAction("Show", new { id = UserCommunity.CommunityId });
        }



        [Authorize(Roles = "User,Admin")]
        public IActionResult KickMember(int userCommunityId)
        {
            var UserCommunity = db.UserCommunities
                                .Include(uc => uc.Community)
                                .FirstOrDefault(uc => uc.Id == userCommunityId);

            if (UserCommunity == null)
                return NotFound();

            var CurrentUser = _userManager.GetUserAsync(User).Result;

            var CurrentMembership = db.UserCommunities
                                      .FirstOrDefault(uc => uc.CommunityId == UserCommunity.CommunityId &&
                                                            uc.UserId == CurrentUser.Id);


            if (CurrentMembership == null) return Unauthorized();

            bool CurrentUserAdmin = CurrentMembership.Role == CommunityRole.Admin;
            bool CurrentUserModerator = CurrentMembership.Role == CommunityRole.Moderator;

            // nu poti sa ti dai auto-kick
            if (UserCommunity.UserId == CurrentUser.Id)
            {
                TempData["message"] = "Nu puteti sa va dati kick. folositi butonul leave (cand o sa existe)";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", new { id = UserCommunity.CommunityId });
            }

            // nu poti da kick la admin daca nu esti admin
            if (UserCommunity.Role == CommunityRole.Admin && !CurrentUserAdmin)
            {
                TempData["message"] = "Nu puteti da kick la un administrator";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", new { id = UserCommunity.CommunityId });
            }

            // moderatorii pot da kick doar membrilor
            if (CurrentUserModerator && UserCommunity.Role != CommunityRole.Member)
            {
                TempData["message"] = "Moderatorii pot scoate doar membrii.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", new { id = UserCommunity.CommunityId });
            }


            // kick permis
            db.UserCommunities.Remove(UserCommunity);
            db.SaveChanges();

            TempData["message"] = "Membru scos cu succes!";
            TempData["messageType"] = "alert-success";
            return RedirectToAction("Show", new { id = UserCommunity.CommunityId });
        }


        [Authorize(Roles = "User,Admin")]
        public IActionResult Join(int communityId)
        {
            var CurrentUser = _userManager.GetUserAsync(User).Result;

            if (CurrentUser == null)
            {
                return Unauthorized();
            }


            //verificam daca comunitatea exista
            var CommunityExists = db.Communities.Any(c => c.Id == communityId);

            if (!CommunityExists)
            {
                return NotFound();
            }


            // verificam daca userul este deja membru
            bool IsMember = db.UserCommunities
                              .Any(uc => uc.CommunityId == communityId &&
                                         uc.UserId == CurrentUser.Id);

            if (IsMember)
            {
                TempData["message"] = "Sunteti deja membru al acestei comunitati.";
                TempData["messageType"] = "alert-info";
                return RedirectToAction("Show", new { id = communityId });
            }

            else
            {
                var NewMember = new UserCommunity()
                {
                    UserId = CurrentUser.Id,
                    CommunityId = communityId,
                    JoinedAt = DateTime.Now,
                    Role = CommunityRole.Member
                };

                db.UserCommunities.Add(NewMember);
                db.SaveChanges();
            }

            TempData["message"] = "Ai intrat in comunitate";
            TempData["messageType"] = "alert-success";
            return RedirectToAction("Show", new { id = communityId });

        }
    }

}

