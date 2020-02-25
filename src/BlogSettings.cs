namespace Miniblog.Core
{
    /// <summary>
    /// The BlogSettings class.
    /// </summary>
    public class BlogSettings
    {
        /// <summary>
        /// Gets or sets the comments close after days.
        /// </summary>
        /// <value>The comments close after days.</value>
        public int CommentsCloseAfterDays { get; set; } = 10;

        /// <summary>
        /// Gets or sets the ListView.
        /// </summary>
        /// <value>The ListView.</value>
        public PostListView ListView { get; set; } = PostListView.TitlesAndExcerpts;

        /// <summary>
        /// Gets or sets the owner.
        /// </summary>
        /// <value>The owner.</value>
        public string Owner { get; set; } = "The Owner";

        /// <summary>
        /// Gets or sets the posts per page.
        /// </summary>
        /// <value>The posts per page.</value>
        public int PostsPerPage { get; set; } = 4;
    }
}
