using System;
using System.Linq;

namespace Miniblog.Core.Models
{
    public class Slug
    {
        public string ID { get; set; }

        internal DateTime Date { get; set; }

        internal string DefaultSlug { get; set; }

        public bool IsGuidOrLongId()
        {
            return ID != null && (Guid.TryParse(ID, out Guid g) || long.TryParse(ID, out long l));
        }

        public bool StartsWithDate()
        {
            if (ID != null && ID.Length > 8 && int.TryParse(new string(ID.Take(8).ToArray()), out int i))
            {
                var dateSlug = string.Join("/", i.ToString().Substring(0, 4), i.ToString().Substring(4, 2), i.ToString().Substring(6, 2));

                if (DateTime.TryParse(dateSlug, out DateTime date))
                {
                    Date = date;
                    DefaultSlug = ID.Substring(8);

                    return true;
                }
            }
            return false;
        }
    }
}