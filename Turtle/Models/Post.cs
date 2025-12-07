using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Turtle.Models
{
    public class Post
    {
        [Key]
        public int Id { get; set; }

        public string? Title { get; set; }

        [Required]
        public string? Content {  get; set; }

        public int Likes { get; set; } = 0;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }

        public int? CommunityId { get; set; }
        public virtual Community? Community { get; set; }

        public int? MotherPostId { get; set; }
        public virtual Post? MotherPost { get; set; }
        public virtual ICollection<Post> Comments { get; set;  } = [];


        public virtual ICollection<PostLike> PostLikes { get; set; } = [];
        public virtual ICollection<PostCategory> PostCategories { get; set; } = [];
        
        [NotMapped]
        public bool Liked { get; set; }
    }
}
