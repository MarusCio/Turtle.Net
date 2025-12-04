using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Turtle.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Category name is required!")]
        public string CategoryName { get; set; }

        [Required(ErrorMessage = "NSFW is required!")]
        public bool NSFW { get; set; }


    }
}
