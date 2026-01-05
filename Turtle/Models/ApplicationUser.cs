using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Turtle.Models
{
    public class ApplicationUser : IdentityUser
    {
        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        public DateTime? DateOfBirth { get; set; }

        public string? ProfileImageUrl { get; set; }

        public virtual ICollection<UserCommunity> UserCommunities { get; set; } = [];

        public virtual ICollection<Post> Posts { get; set; } = [];
        public virtual ICollection<PostLike> PostsLiked { get; set; } = []; 

        public virtual ICollection<UserFollow> Followers { get; set; } = [];
        public virtual ICollection<UserFollow> Following { get; set; } = [];
    }
}
