namespace Miniblog.Core
{
    public class BlogSettings
    {
        public string Name { get; set; } = "Miniblog.Core";
        public string Description { get; set; } = "A short description of the blog";
        public string Owner { get; set; } = "The Owner";
        public bool AllowHtml { get; set; } = false;
        public int PostsPerPage { get; set; } = 2;
        public int CommentsCloseAfterDays { get; set; } = 10;
    }
}
