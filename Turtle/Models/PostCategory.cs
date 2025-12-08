using Microsoft.Build.Framework;
using System.ComponentModel.DataAnnotations.Schema;

namespace Turtle.Models
{
    public class PostCategory
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int? PostId { get; set; }
        public virtual Post? Post { get; set; }
        public int? CategoryId { get; set; }
        public virtual Category? Category { get; set; }
    }
}
