using Microsoft.AspNetCore.Http;
using Miniblog.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Miniblog.Core
{
    public interface IBlogService
    {
        /// <summary>
        /// Gets paginated posts
        /// </summary>
        /// <param name="count">page size</param>
        /// <param name="skip">number of posts to skip</param>
        /// <returns>paginated posts</returns>
        Task<IEnumerable<Post>> GetPosts(int count, int skip = 0);

        /// <summary>
        /// Gets paginated posts w.r.t category filter.
        /// </summary>
        /// <param name="category">category to filter to</param>
        /// <param name="count">page size</param>
        /// <param name="skip">number of posts to skip</param>
        /// <returns>paginated posts w.r.t category filter</returns>
        Task<IEnumerable<Post>> GetPostsByCategory(string category, int count, int skip = 0);

        /// <summary>
        /// Total number of posts w.r.t. category, if provided
        /// </summary>
        /// <param name="category">Category to return count. If string.IsNullOrEmpty, then return count for all posts.</param>
        /// <returns>Number of posts, filtered by category if provided</returns>
        Task<int> GetPostCount(string category = null);

        /// <summary>
        /// Return post by slug
        /// </summary>
        /// <param name="slug">slug to search for</param>
        /// <returns>post if found, null otherwise</returns>
        /// <remarks>Slug is the post name that appears in the url</remarks>
        Task<Post> GetPostBySlug(string slug);

        /// <summary>
        /// Return post by id
        /// </summary>
        /// <param name="id">id to search for</param>
        /// <returns>post if found, null otherwise</returns>
        Task<Post> GetPostById(string id);

        /// <summary>
        /// Categories in use, with viewable posts by user
        /// </summary>
        /// <returns>Filtered list of categories to what's available by the user</returns>
        Task<IEnumerable<string>> GetCategories();

        /// <summary>
        /// Save post and update children (comments and post categories), creates categories if necessary
        /// </summary>
        /// <param name="post">Post to save</param>
        /// <returns>awaitable task</returns>
        Task SavePost(Post post);

        /// <summary>
        /// Removes post, deletes children, and deletes abandoned categories if applicable
        /// </summary>
        /// <param name="post">post to delete</param>
        /// <returns>awaitable task</returns>
        Task DeletePost(Post post);

        Task<string> SaveFile(byte[] bytes, string fileName, string suffix = null);
    }

    public abstract class InMemoryBlogServiceBase : IBlogService
    {
        public InMemoryBlogServiceBase(IHttpContextAccessor contextAccessor)
        {
            ContextAccessor = contextAccessor;
        }

        protected List<Post> Cache { get; set; }
        protected IHttpContextAccessor ContextAccessor { get; }

        public virtual Task<IEnumerable<Post>> GetPosts(int count, int skip = 0)
        {
            bool isAdmin = IsAdmin();

            var posts = Cache
                .Where(p => p.PubDate <= DateTime.UtcNow && (p.IsPublished || isAdmin))
                .Skip(skip)
                .Take(count);

            return Task.FromResult(posts);
        }

        public virtual Task<IEnumerable<Post>> GetPostsByCategory(string category, int count, int skip = 0)
        {
            bool isAdmin = IsAdmin();

            var posts = Cache
                .Where(p => p.PubDate <= DateTime.UtcNow && (p.IsPublished || isAdmin)
                    && p.PostCategories.Any(pc => string.Equals(pc.CategoryID, category, StringComparison.OrdinalIgnoreCase)))
                .Skip(skip)
                .Take(count);

            return Task.FromResult(posts);

        }

        public virtual Task<int> GetPostCount(string category = null)
        {
            bool isAdmin = IsAdmin();

            var count = Cache
                .Where(p => p.PubDate <= DateTime.UtcNow && (p.IsPublished || isAdmin)
                    && (
                        string.IsNullOrEmpty(category)
                        || p.PostCategories.Any(pc => string.Equals(pc.CategoryID, category, StringComparison.OrdinalIgnoreCase)))
                    )
                .Count();

            return Task.FromResult(count);
        }

        public virtual Task<Post> GetPostBySlug(string slug)
        {
            var post = Cache.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
            bool isAdmin = IsAdmin();

            if (post != null && post.PubDate <= DateTime.UtcNow && (post.IsPublished || isAdmin))
            {
                return Task.FromResult(post);
            }

            return Task.FromResult<Post>(null);
        }

        public virtual Task<Post> GetPostById(string id)
        {
            var post = Cache.FirstOrDefault(p => p.ID.Equals(id, StringComparison.OrdinalIgnoreCase));
            bool isAdmin = IsAdmin();

            if (post != null && post.PubDate <= DateTime.UtcNow && (post.IsPublished || isAdmin))
            {
                return Task.FromResult(post);
            }

            return Task.FromResult<Post>(null);
        }

        public virtual Task<IEnumerable<string>> GetCategories()
        {
            bool isAdmin = IsAdmin();

            var categories = Cache
                .Where(p => p.IsPublished || isAdmin)
                .SelectMany(post => post.PostCategories)
                .Select(cat => cat.CategoryID.ToLowerInvariant())
                .Distinct();

            return Task.FromResult(categories);
        }

        public abstract Task SavePost(Post post);

        public abstract Task DeletePost(Post post);

        public abstract Task<string> SaveFile(byte[] bytes, string fileName, string suffix = null);

        protected void SortCache()
        {
            Cache.Sort((p1, p2) => p2.PubDate.CompareTo(p1.PubDate));
        }

        protected bool IsAdmin()
        {
            return ContextAccessor.HttpContext?.User?.Identity.IsAuthenticated == true;
        }
    }
}
