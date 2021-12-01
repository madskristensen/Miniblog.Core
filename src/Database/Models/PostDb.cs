namespace Miniblog.Core.Database.Models
{
    using System;
    using System.Collections.Generic;

    public class PostDb
    {
        public Guid ID { get; set; }

        public ICollection<CommentDb>? Comments { get; set; }

        public string? Content { get; set; }

        public string? Excerpt { get; set; }

        public bool IsPublished { get; set; }

        public DateTime LastModified { get; set; }

        public DateTime PubDate { get; set; }

        public string? Slug { get; set; }

        public string? Title { get; set; }

        public virtual ICollection<CategoryDb>? Categories { get; set; }

        public virtual ICollection<TagDb>? Tags { get; set; }
    }
}
