using Miniblog.Core.Models;

namespace Miniblog.Core.Services;

/// <summary>
/// Provides blog-related operations such as managing posts, categories, tags, and files.
/// </summary>
public interface IBlogService
{
    /// <summary>
    /// Deletes the specified blog post.
    /// </summary>
    /// <param name="post">The post to delete.</param>
    Task DeletePost(Post post);

    /// <summary>
    /// Gets all categories used in blog posts.
    /// </summary>
    /// <returns>An asynchronous sequence of category names.</returns>
    IAsyncEnumerable<string> GetCategories();

    /// <summary>
    /// Gets a post by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the post.</param>
    /// <returns>The post if found; otherwise, <c>null</c>.</returns>
    Task<Post?> GetPostById(string id);

    /// <summary>
    /// Gets a post by its slug.
    /// </summary>
    /// <param name="slug">The slug of the post.</param>
    /// <returns>The post if found; otherwise, <c>null</c>.</returns>
    Task<Post?> GetPostBySlug(string slug);

    /// <summary>
    /// Gets all blog posts.
    /// </summary>
    /// <returns>An asynchronous sequence of posts.</returns>
    IAsyncEnumerable<Post> GetPosts();

    /// <summary>
    /// Gets a subset of blog posts with pagination.
    /// </summary>
    /// <param name="count">The number of posts to retrieve.</param>
    /// <param name="skip">The number of posts to skip.</param>
    /// <returns>An asynchronous sequence of posts.</returns>
    IAsyncEnumerable<Post> GetPosts(int count, int skip = 0);

    /// <summary>
    /// Gets all posts in the specified category.
    /// </summary>
    /// <param name="category">The category name.</param>
    /// <returns>An asynchronous sequence of posts in the category.</returns>
    IAsyncEnumerable<Post> GetPostsByCategory(string category);

    /// <summary>
    /// Gets all posts with the specified tag.
    /// </summary>
    /// <param name="tag">The tag name.</param>
    /// <returns>An asynchronous sequence of posts with the tag.</returns>
    IAsyncEnumerable<Post> GetPostsByTag(string tag);

    /// <summary>
    /// Gets all tags used in blog posts.
    /// </summary>
    /// <returns>An asynchronous sequence of tag names.</returns>
    IAsyncEnumerable<string> GetTags();

    /// <summary>
    /// Saves a file and returns its storage path or URL.
    /// </summary>
    /// <param name="bytes">The file contents as a byte array.</param>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="suffix">An optional suffix to append to the file name.</param>
    /// <returns>The path or URL of the saved file.</returns>
    Task<string> SaveFile(byte[] bytes, string fileName, string? suffix = null);

    /// <summary>
    /// Saves the specified blog post.
    /// </summary>
    /// <param name="post">The post to save.</param>
    Task SavePost(Post post);
}
