using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Miniblog.Core
{
    public class Post
    {
        [JsonIgnore]
        [Required]
        public string ID { get; set; } = DateTime.UtcNow.Ticks.ToString();

        [Required]
        public string Title { get; set; }

        [Required]
        public string Slug { get; set; }

        [Required]
        public string Excerpt { get; set; } 

        [Required]
        public string Content { get; set; }

        public DateTime PubDate { get; set; }

        public DateTime LastModified { get; set; }

        public bool IsPublished { get; set; } = true;

        public IList<string> Categories { get; set; } = new List<string>();

        public IList<Comment> Comments { get; } = new List<Comment>();

        public string GetLink()
        {
            return $"/post/{Slug}/";
        }
    }
}
