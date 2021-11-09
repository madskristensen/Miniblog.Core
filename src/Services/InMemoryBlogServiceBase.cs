namespace Miniblog.Core.Services
{
    using Microsoft.AspNetCore.Http;

    using Miniblog.Core.Models;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;

    public abstract class InMemoryBlogServiceBase : IBlogService
    {
        protected InMemoryBlogServiceBase(IHttpContextAccessor contextAccessor) => this.ContextAccessor = contextAccessor;

        protected List<Post> Cache { get; } = new List<Post>();

        protected IHttpContextAccessor ContextAccessor { get; }

        public abstract Task DeletePost(Post post);

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

        [SuppressMessage(
            "Globalization",
            "CA1308:Normalize strings to uppercase",
            Justification = "Consumer preference.")]
        public virtual IAsyncEnumerable<string> GetTags()
        {
            var isAdmin = this.IsAdmin();

            var tags = this.Cache
                .Where(p => p.IsPublished || isAdmin)
                .SelectMany(post => post.Tags)
                .Select(tag => tag.ToLowerInvariant())
                .Distinct()
                .ToAsyncEnumerable();

            return tags;
        }

        public virtual Task<Post?> GetPostById(string id)
        {
            var isAdmin = this.IsAdmin();
            var post = this.Cache.FirstOrDefault(p => p.ID.Equals(id, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(
                post is null || !post.IsVisible() || !isAdmin
                ? null
                : post);
        }

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

        public virtual IAsyncEnumerable<Post> GetPostsByCategory(string category)
        {
            var isAdmin = this.IsAdmin();

            var posts = from p in this.Cache
                        where p.IsVisible() || isAdmin
                        where p.Categories.Contains(category, StringComparer.OrdinalIgnoreCase)
                        select p;

            return posts.ToAsyncEnumerable();
        }

        public virtual IAsyncEnumerable<Post> GetPostsByTag(string tag)
        {
            var isAdmin = this.IsAdmin();

            var posts = from p in this.Cache
                        where p.IsVisible() || isAdmin
                        where p.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)
                        select p;

            return posts.ToAsyncEnumerable();
        }

        public abstract Task<string> SaveFile(byte[] bytes, string fileName, string? suffix = null);

        public abstract Task SavePost(Post post);

        protected bool IsAdmin() => this.ContextAccessor.HttpContext?.User?.Identity.IsAuthenticated ?? false;

        protected void SortCache() => this.Cache.Sort((p1, p2) => p2.PubDate.CompareTo(p1.PubDate));
    }
}
