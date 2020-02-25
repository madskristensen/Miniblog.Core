namespace Miniblog.Core.Services
{
    using Microsoft.AspNetCore.Http;

    using Miniblog.Core.Models;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// The InMemoryBlogServiceBase class. Implements the <see
    /// cref="Miniblog.Core.Services.IBlogService" />
    /// </summary>
    /// <seealso cref="Miniblog.Core.Services.IBlogService" />
    public abstract class InMemoryBlogServiceBase : IBlogService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryBlogServiceBase" /> class.
        /// </summary>
        /// <param name="contextAccessor">The context accessor.</param>
        protected InMemoryBlogServiceBase(IHttpContextAccessor contextAccessor) => this.ContextAccessor = contextAccessor;

        /// <summary>
        /// Gets or sets the cache.
        /// </summary>
        /// <value>The cache.</value>
        protected List<Post> Cache { get; } = new List<Post>();

        /// <summary>
        /// Gets the context accessor.
        /// </summary>
        /// <value>The context accessor.</value>
        protected IHttpContextAccessor ContextAccessor { get; }

        /// <summary>
        /// Deletes the post.
        /// </summary>
        /// <param name="post">The post.</param>
        /// <returns>Task.</returns>
        public abstract Task DeletePost(Post post);

        /// <summary>
        /// Gets the categories.
        /// </summary>
        /// <returns>Task&lt;IEnumerable&lt;System.String&gt;&gt;.</returns>
        [SuppressMessage(
            "Globalization",
            "CA1308:Normalize strings to uppercase",
            Justification = "Consumer preference.")]
        public virtual IAsyncEnumerable<string> GetCategories()
        {
            var isAdmin = this.IsAdmin();

            var categories = this.Cache
                .Where(p => p.IsPublished || isAdmin)
                .SelectMany(post => post.Categories)
                .Select(cat => cat.ToLowerInvariant())
                .Distinct()
                .ToAsyncEnumerable();

            return categories;
        }

        /// <summary>
        /// Gets the post by identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Task&lt;Post&gt;.</returns>
        public virtual Task<Post?> GetPostById(string id)
        {
            var isAdmin = this.IsAdmin();
            var post = this.Cache.FirstOrDefault(p => p.ID.Equals(id, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(
                post is null || !post.IsVisible() || !isAdmin
                ? null
                : post);
        }

        /// <summary>
        /// Gets the post by slug.
        /// </summary>
        /// <param name="slug">The slug.</param>
        /// <returns>Task&lt;Post&gt;.</returns>
        public virtual Task<Post?> GetPostBySlug(string slug)
        {
            var isAdmin = this.IsAdmin();
            var post = this.Cache.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(
                post is null || !post.IsVisible() || !isAdmin
                ? null
                : post);
        }

        /// <remarks>Overload for getPosts method to retrieve all posts.</remarks>
        public virtual IAsyncEnumerable<Post> GetPosts()
        {
            var isAdmin = this.IsAdmin();
            return this.Cache.Where(p => p.IsVisible() || isAdmin).ToAsyncEnumerable();
        }

        /// <summary>
        /// Gets the posts.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <param name="skip">The skip.</param>
        /// <returns>Task&lt;IEnumerable&lt;Post&gt;&gt;.</returns>
        public virtual IAsyncEnumerable<Post> GetPosts(int count, int skip = 0)
        {
            var isAdmin = this.IsAdmin();

            var posts = this.Cache
                .Where(p => p.IsVisible() || isAdmin)
                .Skip(skip)
                .Take(count)
                .ToAsyncEnumerable();

            return posts;
        }

        /// <summary>
        /// Gets the posts by category.
        /// </summary>
        /// <param name="category">The category.</param>
        /// <returns>Task&lt;IEnumerable&lt;Post&gt;&gt;.</returns>
        public virtual IAsyncEnumerable<Post> GetPostsByCategory(string category)
        {
            var isAdmin = this.IsAdmin();

            var posts = from p in this.Cache
                        where p.IsVisible() || isAdmin
                        where p.Categories.Contains(category, StringComparer.OrdinalIgnoreCase)
                        select p;

            return posts.ToAsyncEnumerable();
        }

        /// <summary>
        /// Saves the file.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="suffix">The suffix.</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        public abstract Task<string> SaveFile(byte[] bytes, string fileName, string? suffix = null);

        /// <summary>
        /// Saves the post.
        /// </summary>
        /// <param name="post">The post.</param>
        /// <returns>Task.</returns>
        public abstract Task SavePost(Post post);

        /// <summary>
        /// Determines whether this instance is admin.
        /// </summary>
        /// <returns><c>true</c> if this instance is admin; otherwise, <c>false</c>.</returns>
        protected bool IsAdmin() => this.ContextAccessor.HttpContext?.User?.Identity.IsAuthenticated ?? false;

        /// <summary>
        /// Sorts the cache.
        /// </summary>
        protected void SortCache() => this.Cache.Sort((p1, p2) => p2.PubDate.CompareTo(p1.PubDate));
    }
}
