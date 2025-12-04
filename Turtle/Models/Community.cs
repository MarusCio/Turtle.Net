using System.ComponentModel.DataAnnotations;

namespace Turtle.Models

{
    public class Community
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Community name is required!")]
        [MinLength(5,ErrorMessage = "Community name must have at least 5 characters!")]
        [StringLength(30, ErrorMessage = "Community name can not have more than 30 characters!")]
        public string CommunityName { get; set; } 

        public string? CreatorId { get; set; }

        public virtual ApplicationUser? Creator { get; set; }

        [Required(ErrorMessage = "NSFW is required!")]
        public bool NSFW { get; set; }

        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<UserCommunity> UserCommunities { get; set; } = [];
    }
}
