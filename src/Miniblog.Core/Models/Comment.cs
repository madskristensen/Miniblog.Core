using System;

namespace Miniblog.Core
{
    public class Comment
    {
        public string ID { get; set; } = Guid.NewGuid().ToString();
        public string Author { get; set; }
        public string Email { get; set; }
        public string Content { get; set; }
        public DateTime PubDate { get; set; } = DateTime.UtcNow;
        public bool IsAdmin { get; set; }
    }
}
