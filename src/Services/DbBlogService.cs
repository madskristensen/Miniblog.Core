namespace Miniblog.Core.Services
{
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;

    using Miniblog.Core.Database;
    using Miniblog.Core.Models;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class DbBlogService : IBlogService
    {
        private readonly IWebHostEnvironment env;
        private readonly IHttpContextAccessor contextAccessor;
        private readonly BlogContext blogContext;

        public DbBlogService(
            IWebHostEnvironment env,
            IHttpContextAccessor contextAccessor,
            BlogContext blogContext)
        {
            if (env is null)
            {
                throw new ArgumentNullException(nameof(env));
            }
            this.env = env;
            this.contextAccessor = contextAccessor;
            this.blogContext = blogContext;
        }

        public async Task DeletePost(Post post)
        {
            if (post is null)
            {
                throw new ArgumentNullException(nameof(post));
            }

            if (!Guid.TryParse(post.ID, out var postId))
            {
                throw new ArgumentException("wrong format of post id");
            }

            var postEntity = await this.blogContext.Posts.FindAsync(postId);
            if (postEntity != null)
            {
                this.blogContext.Remove(postEntity);
            }
        }

        public IAsyncEnumerable<string> GetCategories()
        {
            var isAdmin = this.IsAdmin();

            return this.blogContext
                .Categories
                .Where(p => p.Post.IsPublished || isAdmin)
                .Select(cat => cat.Name.ToLowerInvariant())
                .Distinct()
                .ToAsyncEnumerable();
        }

        public IAsyncEnumerable<string> GetTags()
        {
            var isAdmin = this.IsAdmin();

            return this.blogContext
                .Tags
                .Where(p => p.Post.IsPublished || isAdmin)
                .Select(tag => tag.Name.ToLowerInvariant())
                .Distinct()
                .ToAsyncEnumerable();
        }

        public Task<Post?> GetPostById(string id) => throw new NotImplementedException();

        public Task<Post?> GetPostBySlug(string slug) => throw new NotImplementedException();

        public IAsyncEnumerable<Post> GetPosts() => throw new NotImplementedException();

        public IAsyncEnumerable<Post> GetPosts(int count, int skip = 0) => throw new NotImplementedException();
        public IAsyncEnumerable<Post> GetPostsByCategory(string category) => throw new NotImplementedException();
        public IAsyncEnumerable<Post> GetPostsByTag(string tag) => throw new NotImplementedException();
        public Task<string> SaveFile(byte[] bytes, string fileName, string? suffix = null) => throw new NotImplementedException();
        public Task SavePost(Post post) => throw new NotImplementedException();

        protected bool IsAdmin() => this.contextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;
    }
}
