using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Miniblog.Core.Models;
using Miniblog.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace Miniblog.Core.Services
{
    public class DatabaseBlogService : IBlogService
    {
        private readonly MiniblogDbContext _context;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly string _folder;

        public DatabaseBlogService(IHostingEnvironment env, IHttpContextAccessor contextAccessor, MiniblogDbContext context) 
        {
            _folder = Path.Combine(env.WebRootPath, "posts");
            _contextAccessor = contextAccessor;
            _context = context;
        }

        public async Task DeletePost(Post post)
        {
            _context.Posts.Remove(post);
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }

        public Task<IEnumerable<string>> GetCategories()
        {
            bool isAdmin = IsAdmin();

            var categories = _context.Posts
                .Where(p => p.IsPublished || isAdmin)
                .SelectMany(post => post.Categories)
                .Select(cat => cat.ToLowerInvariant())
                .Distinct()
                .AsEnumerable();

            return Task.FromResult(categories);
        }

        public async Task<Post> GetPostById(string id)
        {
            var post = await _context.Posts.FindAsync(id);
            bool isAdmin = IsAdmin();

            if (post != null && post.PubDate <= DateTime.UtcNow && (post.IsPublished || isAdmin))
            {
                return post;
            }

            return null;
        }

        public Task<Post> GetPostBySlug(string slug)
        {
            var post = _context.Posts.SingleOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
            bool isAdmin = IsAdmin();

            if (post != null && post.PubDate <= DateTime.UtcNow && (post.IsPublished || isAdmin))
            {
                return Task.FromResult(post);
            }

            return Task.FromResult<Post>(null);
        }

        public Task<IEnumerable<Post>> GetPosts(int count, int skip = 0)
        {
            bool isAdmin = IsAdmin();

            var posts = _context.Posts
                .Where(p => p.PubDate <= DateTime.UtcNow && (p.IsPublished || isAdmin))
                .Skip(skip)
                .Take(count)
                .AsEnumerable();

            return Task.FromResult(posts);
        }

        public Task<IEnumerable<Post>> GetPostsByCategory(string category)
        {
            bool isAdmin = IsAdmin();

            var posts = _context.Posts
                        .Where(p => p.PubDate <= DateTime.UtcNow 
                            && (p.IsPublished || isAdmin)
                            && p.Categories.Contains(category, StringComparer.OrdinalIgnoreCase))
                        .AsEnumerable();

            return Task.FromResult(posts);
        }

        public async Task<string> SaveFile(byte[] bytes, string fileName, string suffix = null)
        {
            suffix = suffix ?? DateTime.UtcNow.Ticks.ToString();

            string ext = Path.GetExtension(fileName);
            string name = Path.GetFileNameWithoutExtension(fileName);

            string relative = $"files/{name}_{suffix}{ext}";
            string absolute = Path.Combine(_folder, relative);
            string dir = Path.GetDirectoryName(absolute);

            Directory.CreateDirectory(dir);
            using (var writer = new FileStream(absolute, FileMode.CreateNew))
            {
                await writer.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
            }

            return "/posts/" + relative;
        }

        public async Task SavePost(Post post)
        {
            _context.Entry(post).State = string.IsNullOrEmpty(post.ID) 
                ? EntityState.Added 
                : EntityState.Modified;

            await _context.SaveChangesAsync();
        }

        protected bool IsAdmin()
        {
            return _contextAccessor.HttpContext?.User?.Identity.IsAuthenticated == true;
        }
    }
}
