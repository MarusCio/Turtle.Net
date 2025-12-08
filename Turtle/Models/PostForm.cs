using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Turtle.Models
{
    public class PostForm
    {
        public string? Title { get; set; }

        [Required]
        public string? Content { get; set; }

        public int? CommunityId { get; set; }

        [NotMapped]
        public IEnumerable<SelectListItem> AvailableCommunities { get; set; } = [];

        public List<int> SelectedCategoryIds { get; set; } = [];

        public IEnumerable<SelectListItem> AvailableCategories { get; set; } = [];
    }
}
