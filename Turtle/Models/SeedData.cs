using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Turtle.Data;

namespace Turtle.Models
{
    public class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {

                if (!context.Roles.Any(r => r.Name == "Admin"))
                {
                    context.Roles.Add
                    (

                        new IdentityRole
                        {
                            Id = "d535d328-faea-41cd-8225-c641d4015ee4",
                            Name = "Admin",
                            NormalizedName = "Admin".ToUpper()
                        }
                    );
                }

                if (!context.Roles.Any(r => r.Name == "User"))
                {
                    context.Roles.Add
                    (

                        new IdentityRole
                        {
                            Id = "d535d328-faea-41cd-8225-c641d4015ee5",
                            Name = "User",
                            NormalizedName = "User".ToUpper()
                        }
                    );
                }


                var hasher = new PasswordHasher<ApplicationUser>();


                if (!context.Users.Any(u => u.Email == "admin@test.com"))
                {
                    context.Users.Add
                    (
                        new ApplicationUser
                        {
                            Id = "53505f86-0d28-40ef-b4de-de4410fff9a4",
                            // primary key
                            UserName = "admin@test.com",
                            EmailConfirmed = true,
                            NormalizedEmail = "ADMIN@TEST.COM",
                            Email = "admin@test.com",
                            NormalizedUserName = "ADMIN@TEST.COM",
                            PasswordHash = hasher.HashPassword(null, "Admin1!")
                        }


                    );
                }

                if (!context.Users.Any(u => u.Email == "admin2@test.com"))
                {
                    context.Users.Add
                    (
                        new ApplicationUser
                        {
                            Id = "53505f86-0d28-40ef-b4de-de4410fff9b4",
                            // primary key
                            UserName = "admin2@test.com",
                            EmailConfirmed = true,
                            NormalizedEmail = "ADMIN2@TEST.COM",
                            Email = "admin2@test.com",
                            NormalizedUserName = "ADMIN2@TEST.COM",
                            PasswordHash = hasher.HashPassword(null, "Admin2!")
                        }


                    );
                }

                if (!context.Users.Any(u => u.Email == "user@test.com"))
                {
                    context.Users.Add
                    (
                        new ApplicationUser
                        {
                            Id = "53505f86-0d28-40ef-b4de-de4410fff9a5",
                            // primary key
                            UserName = "user@test.com",
                            EmailConfirmed = true,
                            NormalizedEmail = "USER@TEST.COM",
                            Email = "user@test.com",
                            NormalizedUserName = "USER@TEST.COM",
                            PasswordHash = hasher.HashPassword(null, "User1!")
                        }

                    );
                }

                if (!context.Users.Any(u => u.Email == "user2@test.com"))
                {
                    context.Users.Add
                    (
                        new ApplicationUser
                        {
                            Id = "53505f86-0d28-40ef-b4de-de4410fff9b5",
                            // primary key
                            UserName = "user2@test.com",
                            EmailConfirmed = true,
                            NormalizedEmail = "USER2@TEST.COM",
                            Email = "user2@test.com",
                            NormalizedUserName = "USER2@TEST.COM",
                            PasswordHash = hasher.HashPassword(null, "User2!")
                        }

                    );
                }


                // ASOCIEREA USER-ROLE
                if (!context.UserRoles.Any(ur =>
                    ur.UserId == "53505f86-0d28-40ef-b4de-de4410fff9a4" &&
                    ur.RoleId == "d535d328-faea-41cd-8225-c641d4015ee4"))
                {
                    context.UserRoles.Add(new IdentityUserRole<string>
                    {
                        UserId = "53505f86-0d28-40ef-b4de-de4410fff9a4",
                        RoleId = "d535d328-faea-41cd-8225-c641d4015ee4"
                    });
                }

                // Admin2 → Admin
                if (!context.UserRoles.Any(ur =>
                    ur.UserId == "53505f86-0d28-40ef-b4de-de4410fff9b4" &&
                    ur.RoleId == "d535d328-faea-41cd-8225-c641d4015ee4"))
                {
                    context.UserRoles.Add(new IdentityUserRole<string>
                    {
                        UserId = "53505f86-0d28-40ef-b4de-de4410fff9b4",
                        RoleId = "d535d328-faea-41cd-8225-c641d4015ee4"
                    });
                }

                // User1 → User
                if (!context.UserRoles.Any(ur =>
                    ur.UserId == "53505f86-0d28-40ef-b4de-de4410fff9a5" &&
                    ur.RoleId == "d535d328-faea-41cd-8225-c641d4015ee5"))
                {
                    context.UserRoles.Add(new IdentityUserRole<string>
                    {
                        UserId = "53505f86-0d28-40ef-b4de-de4410fff9a5",
                        RoleId = "d535d328-faea-41cd-8225-c641d4015ee5"
                    });
                }

                // User2 → User
                if (!context.UserRoles.Any(ur =>
                    ur.UserId == "53505f86-0d28-40ef-b4de-de4410fff9b5" &&
                    ur.RoleId == "d535d328-faea-41cd-8225-c641d4015ee5"))
                {
                    context.UserRoles.Add(new IdentityUserRole<string>
                    {
                        UserId = "53505f86-0d28-40ef-b4de-de4410fff9b5",
                        RoleId = "d535d328-faea-41cd-8225-c641d4015ee5"
                    });
                }



                // seed categories
                if (!context.Categories.Any())
                {
                    context.Categories.AddRange
                    (
                        new Category
                        {
                            CategoryName = "Comedy",
                            NSFW = false
                        },

                        new Category
                        {
                            CategoryName = "Action",
                            NSFW = false
                        },

                        new Category
                        {
                            CategoryName = "Gore",
                            NSFW = true

                        },

                        new Category
                        {
                            CategoryName = "Horror",
                            NSFW = false
                        },

                        new Category
                        {
                            CategoryName = "Music",
                            NSFW = false
                        },

                        new Category
                        {
                            CategoryName = "Gaming",
                            NSFW = false
                        }


                    );
                }



                //// seed posts

                //Posts without MOTHERPOSTID 
                if (!context.Posts.Any())
                {
                    context.Posts.AddRange
                    (
                        //0
                        new Post
                        {
                            Title = "TestPost",
                            Content = "Buna sunt Stefan, dar de fapt e Marius",
                            Likes = 0,
                            CreatedAt = new DateTime(2005, 10, 10),
                            UserId = "53505f86-0d28-40ef-b4de-de4410fff9a4",
                            CommunityId = null,
                            MotherPostId = null


                        },

                        //1
                        new Post
                        {
                            Title = "LongPost",
                            Content = "Everything that you thought had meaning: every hope, dream, or moment of happiness. None of it matters as you lie bleeding out on the battlefield. None of it changes what a speeding rock does to a body, we all die. But does that mean our lives are meaningless? Does that mean that there was no point in our being born? Would you say that of our slain comrades? What about their lives? Were they meaningless?... They were not! Their memory serves as an example to us all! The courageous fallen! The anguished fallen! Their lives have meaning because we the living refuse to forget them! And as we ride to certain death, we trust our successors to do the same for us! Because my soldiers do not buckle or yield when faced with the cruelty of this world! My soldiers push forward! My soldiers scream out! My soldiers RAAAAAGE!",
                            Likes = 0,
                            CreatedAt = new DateTime(2007, 1, 1),
                            UserId = "53505f86-0d28-40ef-b4de-de4410fff9a5",
                            CommunityId = null,
                            MotherPostId = null


                        },

                        //2
                        new Post
                        {
                            Title = "New Title",
                            Content = "Buna sunt Marius, de data asta fara Stefan",
                            Likes = 0,
                            CreatedAt = DateTime.Now,
                            UserId = "53505f86-0d28-40ef-b4de-de4410fff9b5",
                            CommunityId = null,
                            MotherPostId = null


                        },

                        //3
                        new Post
                        {
                            Title = "ASDASDASD",
                            Content = "ASD",
                            Likes = 0,
                            CreatedAt = DateTime.Now,
                            UserId = "53505f86-0d28-40ef-b4de-de4410fff9b4",
                            CommunityId = null,
                            MotherPostId = null


                        },

                        //4
                        new Post
                        {
                            Title = "ASd",
                            Content = "asd",
                            Likes = 1,
                            CreatedAt = DateTime.Now,
                            UserId = "53505f86-0d28-40ef-b4de-de4410fff9b4",
                            CommunityId = null,
                            MotherPostId = null


                        },

                        //5
                        new Post
                        {
                            Title = "A good film",
                            Content = "Really enjoyed the  variety of elements",
                            Likes = 1,
                            CreatedAt = DateTime.Now,
                            UserId = "53505f86-0d28-40ef-b4de-de4410fff9a4",
                            CommunityId = null,
                            MotherPostId = null


                        },

                        //6
                        new Post
                        {
                            Title = "Yakuza",
                            Content = "Been playing Yakuza dead souls a bit too much",
                            Likes = 0,
                            CreatedAt = DateTime.Now,
                            UserId = "53505f86-0d28-40ef-b4de-de4410fff9a5",
                            CommunityId = null,
                            MotherPostId = null


                        },

                        //7
                        new Post
                        {
                            Title = "Test2",
                            Content = "dasfsdagfadshvfcjh",
                            Likes = 0,
                            CreatedAt = DateTime.Now,
                            UserId = "53505f86-0d28-40ef-b4de-de4410fff9a4",
                            CommunityId = null,
                            MotherPostId = null


                        }


                    );
                    context.SaveChanges();

                }


                // Posts attached to MOTHERPOST
                if (!context.Posts.Any(p => p.MotherPostId != null))  // if there are no comments yet
                {
                    // collect the Ids of the parent posts

                    var parentsPosts = context.Posts
                                .Where(p => p.MotherPostId == null)
                                .OrderBy(p => p.Id)
                                .ToList();

                    if (parentsPosts.Count == 8)
                    {
                        var post1 = parentsPosts[0].Id;
                        var post2 = parentsPosts[1].Id;
                        var post3 = parentsPosts[2].Id;
                        var post4 = parentsPosts[3].Id;
                        var post5 = parentsPosts[4].Id;
                        var post6 = parentsPosts[5].Id;
                        var post7 = parentsPosts[6].Id;
                        var post8 = parentsPosts[7].Id;

                        context.Posts.AddRange
                        (

                            //9
                            new Post
                            {
                                Title = null,
                                Content = "Yesy its good",
                                Likes = 0,
                                CreatedAt = DateTime.Now,
                                UserId = "53505f86-0d28-40ef-b4de-de4410fff9b5",
                                CommunityId = null,
                                MotherPostId = post7


                            },

                            //10
                            new Post
                            {
                                Title = null,
                                Content = "pot sa mi comentez singur :)",
                                Likes = 0,
                                CreatedAt = DateTime.Now,
                                UserId = "53505f86-0d28-40ef-b4de-de4410fff9a5",
                                CommunityId = null,
                                MotherPostId = post7


                            },

                            //12
                            new Post
                            {
                                Title = "Sunt rau!",
                                Content = "si ce te lauzi?",
                                Likes = 0,
                                CreatedAt = DateTime.Now,
                                UserId = "53505f86-0d28-40ef-b4de-de4410fff9a4",
                                CommunityId = null,
                                MotherPostId = post7


                            },

                            //13
                            new Post
                            {
                                Title = null,
                                Content = "fratele meu admin aici de fata",
                                Likes = 0,
                                CreatedAt = DateTime.Now,
                                UserId = "53505f86-0d28-40ef-b4de-de4410fff9b4",
                                CommunityId = null,
                                MotherPostId = post8


                            },

                            //14
                            new Post
                            {
                                Title = null,
                                Content = ":0",
                                Likes = 0,
                                CreatedAt = DateTime.Now,
                                UserId = "53505f86-0d28-40ef-b4de-de4410fff9b5",
                                CommunityId = null,
                                MotherPostId = post1


                            },

                            //15
                            new Post
                            {
                                Title = null,
                                Content = "Nimeni nu citeste ce e acolo",
                                Likes = 0,
                                CreatedAt = DateTime.Now,
                                UserId = "53505f86-0d28-40ef-b4de-de4410fff9a4",
                                CommunityId = null,
                                MotherPostId = post2


                            }

                        );

                        context.SaveChanges();
                    }

                }

                List<int> communityIds = [];
                if (!context.Communities.Any())
                {
                    context.Communities.AddRange
                    (


                       new Community
                       {
                           //Id = 1,
                           CreatorId = "53505f86-0d28-40ef-b4de-de4410fff9a4",
                           CommunityName = "Community 1",
                           Description = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                           NSFW = false,
                           CreatedAt = new DateTime(2020, 2, 5),

                       },

                       new Community
                       {
                           //Id = 2,
                           CreatorId = "53505f86-0d28-40ef-b4de-de4410fff9a5",
                           CommunityName = "Community 2",
                           Description = "All about games",
                           NSFW = false,
                           ImageUrl = "/images/communities/default.jpeg",
                           CreatedAt = new DateTime(2017, 1, 1),

                       },

                       new Community
                       {
                           //Id = 3,
                           CreatorId = "53505f86-0d28-40ef-b4de-de4410fff9b4",
                           CommunityName = "Community 3",
                           Description = "Movies & series discussions",
                           NSFW = false,
                           ImageUrl = "/images/communities/645651c1-d648-460f-86fa-27bf7e998b49.jpeg"
                       },

                       new Community
                       {
                           //Id = 4,
                           CreatorId = "53505f86-0d28-40ef-b4de-de4410fff9b5",
                           CommunityName = "Community 4",
                           Description = "Latest tech news",
                           NSFW = false,
                       },

                       new Community
                       {
                           //Id = 5,
                           CreatorId = "53505f86-0d28-40ef-b4de-de4410fff9a4",
                           CommunityName = "Community 5",
                           Description = "NSFW humor",
                           NSFW = true,
                           ImageUrl = "/images/communities/bcd2f1b5-6755-48b3-8399-640d43b2995f.jpg",
                           CreatedAt = new DateTime(2025, 10, 12),

                       },

                       new Community
                       {
                           //Id = 6,
                           CreatorId = "53505f86-0d28-40ef-b4de-de4410fff9b5",
                           CommunityName = "Comunitatea celor buni la .net",
                           Description = "nu e chiar asa",
                           NSFW = true,
                       },

                        new Community
                        {
                            //Id = 7,
                            CreatorId = "53505f86-0d28-40ef-b4de-de4410fff9b4",
                            CommunityName = "bla bla bla",
                            CreatedAt = new DateTime(2022, 1, 1),

                        }


                    );
                    context.SaveChanges();

                    communityIds = context.Communities
                            .Select(x => x.Id)
                            .ToList();

                    context.UserCommunities.AddRange
                    (
                        new UserCommunity
                        {
                            UserId = "53505f86-0d28-40ef-b4de-de4410fff9a4",
                            CommunityId = communityIds[0],
                            Role = CommunityRole.Admin
                        },
                        new UserCommunity
                        {
                            UserId = "53505f86-0d28-40ef-b4de-de4410fff9b4",
                            CommunityId = communityIds[0],
                            Role = CommunityRole.Member
                        },
                        new UserCommunity
                        {
                            UserId = "53505f86-0d28-40ef-b4de-de4410fff9a5",
                            CommunityId = communityIds[0],
                            Role = CommunityRole.Moderator
                        },

                        new UserCommunity
                        {
                            UserId = "53505f86-0d28-40ef-b4de-de4410fff9a5",
                            CommunityId = communityIds[1],
                            Role = CommunityRole.Admin
                        },

                        new UserCommunity
                        {
                            UserId = "53505f86-0d28-40ef-b4de-de4410fff9b4",
                            CommunityId = communityIds[2],
                            Role = CommunityRole.Admin
                        },

                        new UserCommunity
                        {
                            UserId = "53505f86-0d28-40ef-b4de-de4410fff9b5",
                            CommunityId = communityIds[3],
                            Role = CommunityRole.Admin
                        },

                        new UserCommunity
                        {
                            UserId = "53505f86-0d28-40ef-b4de-de4410fff9a4",
                            CommunityId = communityIds[4],
                            Role = CommunityRole.Admin
                        },

                        new UserCommunity
                        {
                            UserId = "53505f86-0d28-40ef-b4de-de4410fff9b5",
                            CommunityId = communityIds[5],
                            Role = CommunityRole.Admin
                        },

                        new UserCommunity
                        {
                            UserId = "53505f86-0d28-40ef-b4de-de4410fff9b4",
                            CommunityId = communityIds[6],
                            Role = CommunityRole.Admin
                        }
                    );

                    context.SaveChanges();
                }

                context.SaveChanges();
            }
        }
    }
}

