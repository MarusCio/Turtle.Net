using Microsoft.Build.Framework;
using System.ComponentModel.DataAnnotations.Schema;

namespace Turtle.Models

{
    public enum CommunityRole
    {
        Member,
        Moderator,
        Admin
    }
    public class UserCommunity
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }
        public int? CommunityId { get; set; }
        public virtual Community? Community { get; set; }
        public DateTime? JoinedAt { get; set; } = DateTime.Now;
        public CommunityRole? Role { get; set; } = CommunityRole.Member; // "Member", "Moderator", "Admin" 

    }
}
