namespace Miniblog.Core.Services
{
    using Miniblog.Core.Models;

    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// The IBlogService interface.
    /// </summary>
    public interface IBlogService
    {
        /// <summary>
        /// Deletes the post.
        /// </summary>
        /// <param name="post">The post.</param>
        /// <returns>Task.</returns>
        Task DeletePost(Post post);

        /// <summary>
        /// Gets the categories.
        /// </summary>
        /// <returns>IAsyncEnumerable&lt;System.String&gt;.</returns>
        IAsyncEnumerable<string> GetCategories();

        /// <summary>
        /// Gets the post by identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Task&lt;Post&gt;.</returns>
        Task<Post?> GetPostById(string id);

        /// <summary>
        /// Gets the post by slug.
        /// </summary>
        /// <param name="slug">The slug.</param>
        /// <returns>Task&lt;Post&gt;.</returns>
        Task<Post?> GetPostBySlug(string slug);

        /// <summary>
        /// Gets all the posts.
        /// </summary>
        /// <returns>IAsyncEnumerable&lt;Post&gt;.</returns>
        IAsyncEnumerable<Post> GetPosts();

        /// <summary>
        /// Gets the posts. (paged)
        /// </summary>
        /// <param name="count">The count.</param>
        /// <param name="skip">The skip.</param>
        /// <returns>IAsyncEnumerable&lt;Post&gt;.</returns>
        IAsyncEnumerable<Post> GetPosts(int count, int skip = 0);

        /// <summary>
        /// Gets the posts by category.
        /// </summary>
        /// <param name="category">The category.</param>
        /// <returns>IAsyncEnumerable&lt;Post&gt;.</returns>
        IAsyncEnumerable<Post> GetPostsByCategory(string category);

        /// <summary>
        /// Saves the file.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="suffix">The suffix.</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        Task<string> SaveFile(byte[] bytes, string fileName, string? suffix = null);

        /// <summary>
        /// Saves the post.
        /// </summary>
        /// <param name="post">The post.</param>
        /// <returns>Task.</returns>
        Task SavePost(Post post);
    }
}
