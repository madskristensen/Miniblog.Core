namespace Miniblog.Core;

/// <summary>
/// Specifies the view mode for displaying a list of blog posts.
/// </summary>
public enum PostListView
{
    /// <summary>
    /// Display only the titles of the posts.
    /// </summary>
    TitlesOnly,

    /// <summary>
    /// Display the titles and excerpts of the posts.
    /// </summary>
    TitlesAndExcerpts,

    /// <summary>
    /// Display the full content of the posts.
    /// </summary>
    FullPosts
}
