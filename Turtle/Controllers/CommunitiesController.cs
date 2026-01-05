using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Formats.Tar;
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
                             .Include(c => c.UserCommunities)
                             .OrderByDescending(c => c.CreatedAt);

            ViewBag.Communities = communities;

            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];

            }

            var search = "";

            if(Convert.ToString(HttpContext.Request.Query["search"]) is not null)
            {
                search = Convert.ToString(HttpContext.Request.Query["search"]).Trim();

                communities = communities
                              .Where(c => c.CommunityName.Contains(search) ||
                                          c.Creator.UserName.Contains(search))
                              .OrderByDescending(c => c.CreatedAt);
            }

            ViewBag.SearchString = search;

            var sort = Convert.ToString(HttpContext.Request.Query["sort"]);

            if(sort=="popular")
            {
                communities = communities
                              .OrderByDescending(c => c.UserCommunities.Count);
            }

            else
            {
                communities = communities
                              .OrderByDescending(c => c.CreatedAt);
            }

            ViewBag.Sort = sort;

            int _perPage = 3;
            int totalItems = communities.Count();
            var currentPage = Convert.ToInt32(HttpContext.Request.Query["page"]);
            var offset = 0;

            if(!currentPage.Equals(0))
            {
                offset = (currentPage - 1) * _perPage;
            }

            var paginatedCommunities = communities
                                       .Skip(offset)
                                       .Take(_perPage)
                                       .ToList();

            ViewBag.lastPage = Math.Ceiling((float)totalItems / (float)_perPage);
            ViewBag.Communities = paginatedCommunities;

            if (search != "")
            {
                ViewBag.PaginationBaseUrl = "/Communities/Index/?search=" + search + "&sort=" + sort + "&page=";
            }
            else
            {
                ViewBag.PaginationBaseUrl = "/Communities/Index/?sort=" + sort + "&page=";
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

            ViewBag.CurrentUserId = _userManager.GetUserId(User);
            ViewBag.UserIsAdmin = User.IsInRole("Admin");

            
            var Posts = db.Posts
                          .Include(p => p.User)
                          .Include(p => p.PostCategories)
                              .ThenInclude(pc => pc.Category)
                          .Where(p => p.CommunityId == id && p.MotherPostId == null)
                          .OrderByDescending(p => p.CreatedAt);

            foreach (var post in Posts)
            {
                post.Liked = IsPostLiked(post.Id);
            }

            //search postari comunitate
            var PostsSearch = "";

            if (Convert.ToString(HttpContext.Request.Query["searchPosts"]) is not null)
            {
                PostsSearch = Convert.ToString(HttpContext.Request.Query["searchPosts"]).Trim();

                Posts = Posts
                              .Where(p => p.Title.Contains(PostsSearch) ||
                                          p.Content.Contains(PostsSearch) ||
                                          p.PostCategories.Any(pc => pc.Category.CategoryName.Contains(PostsSearch))
                                          )
                              .OrderByDescending(p => p.CreatedAt);
            }

            ViewBag.SearchPosts = PostsSearch;


            //sortare postari
            var sortPosts = Convert.ToString(HttpContext.Request.Query["sortPosts"]);

            if (string.IsNullOrEmpty(sortPosts))
            {
                sortPosts = "recent";
            }

            if (sortPosts == "popular")
            {
                Posts = Posts.OrderByDescending(p => p.Likes)
                            .ThenByDescending(p => p.CreatedAt);
            }
            else if (sortPosts == "oldest")
            {
                Posts = Posts.OrderBy(p => p.CreatedAt);
            }
            else 
            {
                Posts = Posts.OrderByDescending(p => p.CreatedAt);
            }

            ViewBag.SortPosts = sortPosts;


            //search membrii comunitate
            var Members = db.UserCommunities
                            .Include(uc => uc.User)
                            .Where(uc => uc.CommunityId == id)
                            .OrderBy(uc => uc.JoinedAt);

            var MembersSearch = "";

            if (Convert.ToString(HttpContext.Request.Query["searchMembers"]) is not null)
            {
                MembersSearch = Convert.ToString(HttpContext.Request.Query["searchMembers"]).Trim();

                Members = Members
                              .Where(uc => uc.User.UserName.Contains(MembersSearch) ||
                                           uc.Role.ToString().Contains(MembersSearch))
                              .OrderByDescending(c => c.JoinedAt);
            }

            ViewBag.SearchMembers = MembersSearch;

            //paginare postari comunitate
            int _PostsPerPage = 3;
            int TotalPosts = Posts.Count();
            var PostsCurrentPage = Convert.ToInt32(HttpContext.Request.Query["pagePosts"]);
            var PostsOffset = 0;

            if (!PostsCurrentPage.Equals(0))
            {
                PostsOffset = (PostsCurrentPage - 1) * _PostsPerPage;
            }

            var paginatedPosts = Posts
                                   .Skip(PostsOffset)
                                   .Take(_PostsPerPage)
                                   .ToList();

            ViewBag.PostsLastPage = Math.Ceiling((float)TotalPosts / (float)_PostsPerPage);
            ViewBag.Posts = paginatedPosts;


            //paginarea membrii
            int _MembersPerPage = 10;
            int TotalMembers = Members.Count();
            var MembersCurrentPage = Convert.ToInt32(HttpContext.Request.Query["pageMembers"]);
            var MembersOffset = 0;

            if (!MembersCurrentPage.Equals(0))
            {
                MembersOffset = (MembersCurrentPage - 1) * _MembersPerPage;
            }

            var paginatedMembers = Members
                                       .OrderBy(uc => uc.JoinedAt)
                                       .Skip(MembersOffset)
                                       .Take(_MembersPerPage)
                                       .ToList();

            ViewBag.MembersLastPage = Math.Ceiling((float)TotalMembers / (float)_MembersPerPage);
            ViewBag.Members = paginatedMembers;

            //preluam tabul activ
            ViewBag.ActiveTab = HttpContext.Request.Query["tab"].ToString();

            if (string.IsNullOrEmpty(ViewBag.ActiveTab))
            {
                ViewBag.ActiveTab = "posts";
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
        public IActionResult New(Community community, IFormFile? Img)
        {
            community.CreatedAt = DateTime.Now;

            //preluam creatorul comunitatii
            string CreatorId = _userManager.GetUserId(User);
            community.CreatorId = CreatorId;

            community.ImageUrl = "/images/communities/default.jpeg";
            var GoodExtensions = new[] { ".jpeg", ".png", ".jpg", ".webp" };
            const long MaxFile = 2 * 1024 * 1024; //maxim 2MB

            if (Img is not null && Img.Length > 0)
            {
                var extension=Path.GetExtension(Img.FileName).ToLower();

                //verificam tipul pozei
                if(!GoodExtensions.Contains(extension))
                {
                    ModelState.AddModelError("", "Image file not allowed!");
                    return View(community);
                }

                //verificam marimea pozei
                if (Img.Length>MaxFile)
                {
                    ModelState.AddModelError("", "Image sise must be less than 2 MB!");
                    return View(community);
                }

                //salvam imaginea pe server
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/communities");

                //creaza folderul daca nu exista
                Directory.CreateDirectory(uploadsFolder); 

                var fileName = Guid.NewGuid() + Path.GetExtension(Img.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);
                using var fileStream = new FileStream(filePath, FileMode.Create);
                
                Img.CopyTo(fileStream);
                

                //setam URL-ul imaginii in obiectul comunitate
                community.ImageUrl = "/images/communities/" + fileName;
            }

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

        [HttpGet]
        [Authorize(Roles = "User,Admin")]
        public IActionResult Edit(int id)
        {
            Community? community = db.Communities.Find(id);

            if (community is null)
            {
                return NotFound();
            }

            string CurrentUserId = _userManager.GetUserId(User);

            bool CommunityAdmin = db.UserCommunities
                                    .Any(uc =>uc.CommunityId == id &&
                                         uc.UserId == CurrentUserId &&
                                         uc.Role == CommunityRole.Admin);

            //if (community.CreatorId != _userManager.GetUserId(User) && !User.IsInRole("Admin"))
            //{
            //    TempData["message"] = "You do not have permission to edit this community!";
            //    TempData["messageType"] = "alert-danger";
            //    return RedirectToAction("Show", new { id = id });
            //}

            if (!CommunityAdmin)
            {
                TempData["message"] = "You do not have permission to edit this community!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", new { id = id });
            }

            SetAccessRights();

            return View(community);
        }

        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public IActionResult Edit(int id, Community _community, IFormFile? Img)
        {
            Community? community = db.Communities.Find(id);

            if (community is null)
            {
                return NotFound();
            }

            string CurrentUserId = _userManager.GetUserId(User);

            bool CommunityAdmin = db.UserCommunities
                                    .Any(uc =>uc.CommunityId == id &&
                                         uc.UserId == CurrentUserId &&
                                         uc.Role == CommunityRole.Admin);

            //if (community.CreatorId != _userManager.GetUserId(User) && !User.IsInRole("Admin"))
            //{
            //    TempData["message"] = "You do not have permission to edit this community!";
            //    TempData["messageType"] = "alert-danger";
            //    return RedirectToAction("Show", new { id = id });
            //}

            

            if (!CommunityAdmin)
            {
                TempData["message"] = "You do not have permission to edit this community!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", new { id = id });
            }

            var GoodExtensions = new[] { ".jpeg", ".png", ".jpg", ".webp" };
            const long MaxFile = 2 * 1024 * 1024; //maxim 2MB

            if (Img is not null && Img.Length > 0)
            {

                var extension = Path.GetExtension(Img.FileName).ToLower();

                //verificam tipul pozei
                if (!GoodExtensions.Contains(extension))
                {
                    ModelState.AddModelError("", "Image file not allowed!");
                    SetAccessRights();
                    return View(_community);
                }

                //verificam marimea pozei
                if (Img.Length > MaxFile)
                {
                    ModelState.AddModelError("", "Image sise must be less than 2 MB!");
                    SetAccessRights();
                    return View(_community);
                }

                //stergem poza veche daca nu e cea default
                if (!string.IsNullOrEmpty(community.ImageUrl) &&
                    !community.ImageUrl.Contains("default"))
                {
                    var oldImagePath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        community.ImageUrl.TrimStart('/')
                    );

                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }


                //salvam imaginea pe server
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/communities");

                //creaza folderul daca nu exista
                Directory.CreateDirectory(uploadsFolder);

                var fileName = Guid.NewGuid() + Path.GetExtension(Img.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);
                using var fileStream = new FileStream(filePath, FileMode.Create);

                Img.CopyTo(fileStream);

                community.ImageUrl = "/images/communities/" + fileName;
            }

                if (ModelState.IsValid)
            {
                community.CommunityName = _community.CommunityName;
                community.Description = _community.Description;
                community.NSFW = _community.NSFW;


                db.SaveChanges();
                TempData["message"] = "Community updated!";
                TempData["messageType"] = "alert-success";

                return RedirectToAction("Show", new { id = community.Id });

            }

            
            SetAccessRights();
            return View(_community);
        }


        [HttpGet]
        [Authorize(Roles = "User,Admin")]
        public IActionResult Delete(int id)
        {
            Community? community = db.Communities.Find(id);

            if (community is null)
            {
                return NotFound();
            }

            string CurrentUserId = _userManager.GetUserId(User);

            bool CommunityAdmin = db.UserCommunities
                                    .Any(uc => uc.CommunityId == id &&
                                         uc.UserId == CurrentUserId &&
                                         uc.Role == CommunityRole.Admin);

            if (!CommunityAdmin)
            {
                TempData["message"] = "You do not have permission to delete this community!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", new { id = id });
            }

            SetAccessRights();

            return View(community);
        }

        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public IActionResult DeleteCommunity(int id)
        {
            Community? community = db.Communities.Find(id);

            if (community is null)
            {
                return NotFound();
            }

            string CurrentUserId = _userManager.GetUserId(User);

            bool CommunityAdmin = db.UserCommunities
                                    .Any(uc => uc.CommunityId == id &&
                                         uc.UserId == CurrentUserId &&
                                         uc.Role == CommunityRole.Admin);

            if (!CommunityAdmin)
            {
                TempData["message"] = "You do not have permission to delete this community!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", new { id = id });
            }


            EmptyCommunity(id);
            TempData["message"] = "The community has been deleted!";
            TempData["messageType"] = "alert-success";
            return RedirectToAction("Index");

        }


        [HttpPost]
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

            var CurrentUser = _userManager.GetUserAsync(User).Result;

            var CurrentMembership = db.UserCommunities
                                      .FirstOrDefault(uc => uc.CommunityId == UserCommunity.CommunityId &&
                                                            uc.UserId == CurrentUser.Id);

            if (CurrentMembership == null) return Unauthorized();

            //nu poti sa ti schimbi propriul rol
            if (UserCommunity.UserId == CurrentUser.Id)
            {
                TempData["message"] = "You can't change your own role!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", new { id = UserCommunity.CommunityId });
            }

            //nu se poate da acelasi rol unui membru
            if(UserCommunity.Role == newRole)
            {
                TempData["message"] = "The user already has this role.";
                TempData["messageType"] = "alert-info";
                return RedirectToAction("Show", new { id = UserCommunity.CommunityId });
            }

            if (CurrentMembership.Role == CommunityRole.Admin)
            {
                if (UserCommunity.Role == CommunityRole.Admin)
                {
                    TempData["message"] = "You can't modify the admin!";
                    TempData["messageType"] = "alert-danger";
                    return RedirectToAction("Show", new { id = UserCommunity.CommunityId });
                }
            }

            else if (CurrentMembership.Role == CommunityRole.Moderator)
            {
                //moderatorii nu pot modifica rolul adminilor sau al altor moderatori
                if (UserCommunity.Role != CommunityRole.Member)
                {
                    TempData["message"] = "Moderators can only change the role of members!";
                    TempData["messageType"] = "alert-danger";
                    return RedirectToAction("Show", new { id = UserCommunity.CommunityId });
                }

                if (newRole == CommunityRole.Admin)
                {
                    TempData["message"] = "Moderators can't promote a member as admin!";
                    TempData["messageType"] = "alert-danger";
                    return RedirectToAction("Show", new { id = UserCommunity.CommunityId });
                }
            }

            else
            {
                return Unauthorized();
            }


            // se permite schimbarea rolului
            UserCommunity.Role = newRole;
            db.SaveChanges();

            TempData["message"] = "Role updated!";
            TempData["messageType"] = "alert-success";

            return RedirectToAction("Show", new { id = UserCommunity.CommunityId });
        }


        [HttpPost]
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

            if(CurrentMembership.Role != CommunityRole.Admin && CurrentMembership.Role!=CommunityRole.Moderator)
            {
                return Unauthorized();
            }

            //nu poti sa dai auto-kick
            if (UserCommunity.UserId == CurrentUser.Id)
            {
                TempData["message"] = "You can't kick yourself! Use the leave button.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", new { id = UserCommunity.CommunityId });
            }

            //nu poti da kick la admin daca nu esti admin
            if (CurrentMembership.Role == CommunityRole.Admin && UserCommunity.Role==CommunityRole.Admin)
            {
                TempData["message"] = "You can't kick an admin";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", new { id = UserCommunity.CommunityId });
            }

            //moderatorii pot da kick doar membrilor
            if (CurrentMembership.Role==CommunityRole.Moderator && UserCommunity.Role != CommunityRole.Member)
            {
                TempData["message"] = "Moderators can only kick members!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", new { id = UserCommunity.CommunityId });
            }

                     

            //salvam id ul comunitatii
            int communityId = UserCommunity.CommunityId.Value;

            // kick permis
            db.UserCommunities.Remove(UserCommunity);
            db.SaveChanges();

            AutoDeleteCommunity(communityId);


            TempData["message"] = "Member removed!";
            TempData["messageType"] = "alert-success";
            return RedirectToAction("Show", new { id = UserCommunity.CommunityId });
        }

        [HttpPost]
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


            //verificam daca userul este deja membru
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



        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public IActionResult Leave(int communityId)
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


            //verificam daca userul este deja membru
            var IsMember = db.UserCommunities
                              .FirstOrDefault(uc => uc.CommunityId == communityId &&
                                         uc.UserId == CurrentUser.Id);

            if (IsMember == null)
            {
                TempData["message"] = "You are not a member of this community!";
                TempData["messageType"] = "alert-info";
                return RedirectToAction("Show", new { id = communityId });
            }


            int MembersInCommunity = db.UserCommunities
                                        .Count(uc => uc.CommunityId == communityId);


            if(IsMember.Role == CommunityRole.Admin && MembersInCommunity >1)
            {
                //cel mai vechi moderator devine admin
                var NewAdmin = db.UserCommunities
                                 .Where(uc => uc.CommunityId == communityId && uc.Role == CommunityRole.Moderator)
                                 .OrderBy(uc => uc.JoinedAt)
                                 .FirstOrDefault();

                //daca nu exista moderatori se cauta cel mai vechi membru
                if (NewAdmin is null)
                {
                     NewAdmin = db.UserCommunities
                                 .Where(uc => uc.CommunityId == communityId && uc.Role == CommunityRole.Member)
                                 .OrderBy(uc => uc.JoinedAt)
                                 .FirstOrDefault();
                }

                //facem noul admin
                if (NewAdmin != null)
                {
                    NewAdmin.Role = CommunityRole.Admin;
                    db.SaveChanges();
                }
            }

            db.UserCommunities.Remove(IsMember);
            db.SaveChanges();

            AutoDeleteCommunity(communityId);

            TempData["message"] = "You left the community!";
            TempData["messageType"] = "alert-success";
            return RedirectToAction("Index");
        }


        private void EmptyCommunity(int communityId)
        {

            var community = db.Communities.Find(communityId);

            if (community == null)
                return;

            //stergem imaginea (daca nu e default)
            if (!string.IsNullOrEmpty(community.ImageUrl) &&
                !community.ImageUrl.Contains("default"))
            {
                var imagePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    community.ImageUrl.TrimStart('/')
                );

                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            //stergem toti membrii comunitatii
            db.UserCommunities.RemoveRange(
                db.UserCommunities.Where(uc => uc.CommunityId == communityId)
            );
            db.SaveChanges();

            //stergem toate postarile comunitatii
            DeleteCommunityPosts(communityId);
            db.SaveChanges();

            //stergme comunitatea
            if (community != null)
            {
                db.Communities.Remove(community);
            }

            db.SaveChanges();
        }

        private void DeleteCommunityPosts(int communityId)
        {
            var rootPosts = db.Posts
                              .Where(p => p.CommunityId == communityId &&
                                          p.MotherPostId == null)
                              .ToList();

            var stack = new Stack<Post>(rootPosts);
            var postsToDelete = new List<Post>();

            while (stack.Count > 0)
            {
                var post = stack.Pop();
                postsToDelete.Add(post);

                var children = db.Posts
                                 .Where(p => p.MotherPostId == post.Id)
                                 .ToList();

                foreach (var child in children)
                    stack.Push(child);
            }

            if (postsToDelete.Any())
            {
                var postIds = postsToDelete.Select(p => p.Id).ToList();

                db.PostLikes.RemoveRange(
                    db.PostLikes.Where(pl => postIds.Contains(pl.PostId.Value))
                );

                db.PostCategories.RemoveRange(
                    db.PostCategories.Where(pc => postIds.Contains(pc.PostId.Value))
                );

                db.Posts.RemoveRange(postsToDelete);
            }
        }

        private void AutoDeleteCommunity(int communityId)
        {
            bool MembersExist = db.UserCommunities
                                  .Any(uc => uc.CommunityId == communityId);

            if (!MembersExist)
            {
                EmptyCommunity(communityId);
            }
        }



        [NonAction]
        private void SetAccessRights()
        {
            ViewBag.CurrentUserId = _userManager.GetUserId(User);
            ViewBag.UserIsAdmin = User.IsInRole("Admin");
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

    }
}

