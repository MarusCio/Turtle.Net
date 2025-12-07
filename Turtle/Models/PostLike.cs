using Microsoft.Build.Framework;
using System.ComponentModel.DataAnnotations.Schema;

namespace Turtle.Models
{
    public class PostLike
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }
        public int? PostId { get; set; }
        public virtual Post? Post { get; set; }
        public DateTime? LikedAt { get; set; } = DateTime.Now;
    }
}
