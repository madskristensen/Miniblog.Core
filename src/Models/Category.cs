using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Miniblog.Core.Models
{
    public class CategoryCount
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int Count { get; set; } = 0;
    }

    public class Category
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int Count { get; set; } = 0;

        [Required]
        public IList<Post> Posts { get; set; } = new List<Post>();

        [Required]
        public string TagCategory { get; set; } = "smallestTag";
    }
}