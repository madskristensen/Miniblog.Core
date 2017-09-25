using System;

namespace Miniblog.Core
{
    public class Comment
    {
        public string ID { get; set; }
        public string Author { get; set; }
        public string Email { get; set; }
        public string Website { get; set; }
        public string Content { get; set; }
        public DateTime PubDate { get; set; }
        public bool IsAdmin { get; set; }
    }
}
