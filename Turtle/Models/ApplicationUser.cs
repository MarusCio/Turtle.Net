using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Turtle.Models
{
    public class ApplicationUser : IdentityUser
    {
        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        public DateTime? DateOfBirth { get; set; }
        public virtual ICollection<UserCommunity> UserCommunities { get; set; } = [];

        [NotMapped]
        public List<string> Followers { get; set; } = [];

        [NotMapped]
        public List<string> Following { get; set; } = [];
    }
}
