namespace Miniblog.Core
{
    public class BlogSettings
    {
        public string Owner { get; set; } = "Steve Mulholland";
        public int PostsPerPage { get; set; } = 2;
        public int CommentsCloseAfterDays { get; set; } = 10;
    }
}
