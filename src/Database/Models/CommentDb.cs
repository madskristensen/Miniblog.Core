namespace Miniblog.Core.Database.Models
{
    using System;

    public class CommentDb
    {
        public Guid ID { get; set; }

        public string? Author { get; set; }

        public string? Content { get; set; }

        public string? Email { get; set; }

        public bool IsAdmin { get; set; }

        public DateTime PubDate { get; set; }
    }
}
