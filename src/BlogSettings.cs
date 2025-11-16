namespace Miniblog.Core;

/// <summary>
/// Represents the configuration settings for the blog.
/// </summary>
public class BlogSettings
{
    /// <summary>
    /// Gets or sets the number of days after which comments are closed for a post.
    /// </summary>
    public int CommentsCloseAfterDays { get; set; } = 10;

    /// <summary>
    /// Gets or sets a value indicating whether comments should be displayed.
    /// </summary>
    public bool DisplayComments { get; set; } = true;

    /// <summary>
    /// Gets or sets the view mode for displaying a list of blog posts.
    /// </summary>
    public PostListView ListView { get; set; } = PostListView.TitlesAndExcerpts;

    /// <summary>
    /// Gets or sets the name of the blog owner.
    /// </summary>
    public string Owner { get; set; } = "The Owner";

    /// <summary>
    /// Gets or sets the number of posts to display per page.
    /// </summary>
    public int PostsPerPage { get; set; } = 4;
}
