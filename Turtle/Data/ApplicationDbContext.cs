using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Build.Framework;
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
        public DbSet<PostLike> PostLikes { get; set; }
        public DbSet<PostCategory> PostCategories { get; set; }
        public DbSet<UserFollow> UserFollows { get; set; }


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

            // definirea relatiei many to many dintre user si post-uri (like-uri)
            builder.Entity<PostLike>()
                .HasKey(uc => new { uc.Id, uc.UserId, uc.PostId });

            builder.Entity<PostLike>()
                .HasOne(uc => uc.User)
                .WithMany(uc => uc.PostsLiked)
                .HasForeignKey(uc => uc.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<PostLike>()
                .HasOne(uc => uc.Post)
                .WithMany(uc => uc.PostLikes)
                .HasForeignKey(uc => uc.PostId)
                .OnDelete(DeleteBehavior.NoAction);

            // definirea relatiet many to many dintre post si category
            builder.Entity<PostCategory>()
                .HasKey(pc => new { pc.Id, pc.PostId, pc.CategoryId });

            builder.Entity<PostCategory>()
                .HasOne(uc => uc.Post)
                .WithMany(uc => uc.PostCategories)
                .HasForeignKey(uc => uc.PostId);

            builder.Entity<PostCategory>()
                .HasOne(uc => uc.Category)
                .WithMany(uc => uc.PostsCategory)
                .HasForeignKey(uc => uc.CategoryId);

            builder.Entity<UserFollow>()
                .HasOne(uf => uf.Follower)
                .WithMany(u => u.Following)
                .HasForeignKey(uf => uf.FollowerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserFollow>()
                .HasOne(uf => uf.Following)
                .WithMany(u => u.Followers)
                .HasForeignKey(uf => uf.FollowingId)
                .OnDelete(DeleteBehavior.Restrict);

        }

    }
}
