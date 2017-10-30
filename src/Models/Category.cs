using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Miniblog.Core.Models
{
    public class Category
    {
        public string ID { get; set; }

        public IList<PostCategory> PostCategories { get; set; } = new List<PostCategory>();
    }
}
