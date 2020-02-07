using LiteDB;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Miniblog.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Miniblog.Core.Services
{
    public class LiteDatabaseBlogService : BaseBlogService, IBlogService
    {
        private readonly ILiteCollection<Post> _postCollection;
        private List<Post> _postCache;

        public LiteDatabaseBlogService(IWebHostEnvironment env, IHttpContextAccessor contextAccessor) : base(env, contextAccessor)
        {
            const string connectionString = "Filename=Database.db;Password=F3l0ny#&R<Mg6283`2@~!23";
            var liteDb = new LiteDatabase(connectionString);

            _postCollection = liteDb.GetCollection<Post>();

            UpdateCache();
            SortCache();
        }

        public Task<IEnumerable<Post>> GetPosts()
        {
            bool isAdmin = IsAdmin();

            var posts = _postCache
                .Where(p => p.PubDate <= DateTime.Now && (p.IsPublished || isAdmin));

            return Task.FromResult(posts);
        }

        public Task<IEnumerable<Post>> GetPosts(int count, int skip = 0)
        {
            bool isAdmin = IsAdmin();

            var posts = _postCache
                .Where(p => p.PubDate <= DateTime.Now && (p.IsPublished || isAdmin))
                .Skip(skip)
                .Take(count);

            return Task.FromResult(posts);
        }

        public Task<IEnumerable<Post>> GetPostsByCategory(string category)
        {
            bool isAdmin = IsAdmin();

            var posts = from p in _postCache
                where p.PubDate <= DateTime.Now && (p.IsPublished || isAdmin)
                where p.Categories.Contains(category, StringComparer.OrdinalIgnoreCase)
                select p;

            return Task.FromResult(posts);
        }

        public Task<Post> GetPostBySlug(string slug)
        {
            var post = _postCache.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
            bool isAdmin = IsAdmin();

            if (post != null && post.PubDate <= DateTime.Now && (post.IsPublished || isAdmin))
            {
                return Task.FromResult(post);
            }

            return Task.FromResult<Post>(null);
        }

        public Task<Post> GetPostById(string id)
        {
            var post = _postCache.FirstOrDefault(p => p.ID.Equals(id, StringComparison.OrdinalIgnoreCase));
            bool isAdmin = IsAdmin();

            if (post != null && post.PubDate <= DateTime.Now && (post.IsPublished || isAdmin))
            {
                return Task.FromResult(post);
            }

            return Task.FromResult<Post>(null);
        }

        public Task<IEnumerable<string>> GetCategories()
        {
            bool isAdmin = IsAdmin();

            var categories = _postCache
                .Where(p => p.IsPublished || isAdmin)
                .SelectMany(post => post.Categories)
                .Select(cat => cat.ToLowerInvariant())
                .Distinct();

            return Task.FromResult(categories);
        }

        public Task SavePost(Post post)
        {
            post.LastModified = DateTime.Now;

            var taskResult = Task.FromResult(_postCollection.Insert(post));
            UpdateCache();

            return taskResult;
        }

        public Task DeletePost(Post post)
        {
            var taskResult = Task.FromResult(_postCollection.Delete(post.ID));
            UpdateCache();

            return taskResult;
        }

        protected void UpdateCache()
        {
            _postCache = _postCollection.FindAll().ToList();
            SortCache();
        }

        protected void SortCache()
        {
            _postCache.Sort((p1, p2) => p2.PubDate.CompareTo(p1.PubDate));
        }
    }
}