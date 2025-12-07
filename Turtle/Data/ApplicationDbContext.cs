using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Turtle.Models;
namespace Turtle.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<Community> Communities { get; set; }
        public DbSet<UserCommunity> UserCommunities { get; set; }
        public DbSet<Post> Posts { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);


            // definirea relatiei many-many dintre user si community
            builder.Entity<UserCommunity>()
                .HasKey(uc => new { uc.Id, uc.UserId, uc.CommunityId });

            // definirea relatiei cu modelele user si community
            builder.Entity<UserCommunity>()
                .HasOne(uc => uc.User)
                .WithMany(u => u.UserCommunities)
                .HasForeignKey(uc => uc.UserId);

            // definirea relatiei cu modelele community si user
            builder.Entity<UserCommunity>()
                .HasOne(uc => uc.Community)
                .WithMany(c => c.UserCommunities)
                .HasForeignKey(uc => uc.CommunityId);
        }

    }
}
