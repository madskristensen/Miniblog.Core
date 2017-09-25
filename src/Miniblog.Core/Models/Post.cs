using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Miniblog.Core
{
    public class Post
    {
        [JsonIgnore]
        public string ID { get; set; }

        public string Title { get; set; }
        public string Slug { get; set; }
        public string Excerpt { get; set; }
        public string Content { get; set; }
        public DateTime PubDate { get; set; }
        public DateTime LastModified { get; set; }
        public bool IsPublished { get; set; } = true;
        public IList<string> Categories { get; set; }
        public IList<Comment> Comments { get; } = new List<Comment>();

        public string GetLink()
        {
            return $"/post/{Slug}/";
        }
    }
}
