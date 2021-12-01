namespace Miniblog.Core.Database.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class CategoryDb
    {
        public int ID { get; set; }
        public string? Name { get; set; }

        public virtual PostDb? Post { get; set; }

    }
}
