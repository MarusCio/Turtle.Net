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
                
                if (context.Roles.Any())
                {
                    return; 
                }


                context.Roles.AddRange(

                    new IdentityRole
                    {
                        Id = "d535d328-faea-41cd-8225-c641d4015ee4", Name = "Admin", NormalizedName = "Admin".ToUpper() 
                    },
               

                    new IdentityRole
                    {
                        Id = "d535d328-faea-41cd-8225-c641d4015ee5", Name = "User", NormalizedName = "User".ToUpper() 
                    }

                );

                var hasher = new PasswordHasher<ApplicationUser>();

               
                context.Users.AddRange
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
                        PasswordHash = hasher.HashPassword(null,"Admin1!")
                    },

                    new ApplicationUser

                    {
                        Id = "53505f86-0d28-40ef-b4de-de4410fff9a5",
                        // primary key
                        UserName = "user@test.com",
                        EmailConfirmed = true,
                        NormalizedEmail = "USER@TEST.COM",
                        Email = "user@test.com",
                        NormalizedUserName = "USER@TEST.COM",
                        PasswordHash = hasher.HashPassword(null,"User1!")
                    }
                );

                // ASOCIEREA USER-ROLE
                context.UserRoles.AddRange(
                new IdentityUserRole<string>
                {
                    RoleId = "d535d328-faea-41cd-8225-c641d4015ee4",
                    UserId = "53505f86-0d28-40ef-b4de-de4410fff9a4"
                },

                new IdentityUserRole<string>

                {
                    RoleId = "d535d328-faea-41cd-8225-c641d4015ee5",
                    UserId = "53505f86-0d28-40ef-b4de-de4410fff9a5"
                }

                );
                context.SaveChanges();
            }
        }
    }
}

