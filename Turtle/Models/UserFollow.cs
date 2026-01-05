using System.ComponentModel.DataAnnotations;


namespace Turtle.Models
{
    public class UserFollow
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FollowerId { get; set; }
        public virtual ApplicationUser? Follower { get; set; }

        [Required]
        public string FollowingId { get; set; }
        public virtual ApplicationUser? Following { get; set; }
    }
}
