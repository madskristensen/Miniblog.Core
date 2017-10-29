using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Miniblog.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace Miniblog.Core
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

            // remove abandoned categories
            _context.Categories
                .RemoveRange(_context.Categories.Where(cat => !cat.PostCategories.Any()));

            await _context.SaveChangesAsync().ConfigureAwait(false);
        }

        public Task<IEnumerable<string>> GetCategories()
        {
            bool isAdmin = IsAdmin();

            // Note: this doesn't search _context.PostCategories since
            // we want to filter categories to published posts if user
            // is not an admin
            var categories = _context.Posts
                .Where(p => p.IsPublished || isAdmin)
                .SelectMany(p => p.PostCategories)
                .Select(p => p.CategoryID.ToLowerInvariant())
                .Distinct()
                .AsEnumerable();

            return Task.FromResult(categories);
        }

        public Task<Post> GetPostById(string id)
        {
            var post = _context.Posts
                .Include(p => p.PostCategories)
                .Include(p => p.Comments)
                .SingleOrDefault(p => p.ID == id);

            bool isAdmin = IsAdmin();

            if (post != null && post.PubDate <= DateTime.UtcNow && (post.IsPublished || isAdmin))
            {
                return Task.FromResult(post);
            }

            return Task.FromResult<Post>(null);
        }

        public Task<Post> GetPostBySlug(string slug)
        {
            var post = _context.Posts
                .Include(p => p.PostCategories)
                .Include(p => p.Comments)
                .SingleOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

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
                .Include(p => p.PostCategories)
                .Include(p => p.Comments)
                .Where(p => p.PubDate <= DateTime.UtcNow && (p.IsPublished || isAdmin))
                .OrderByDescending(p => p.PubDate)
                .Skip(skip)
                .Take(count)
                .AsEnumerable();

            return Task.FromResult(posts);
        }

        public Task<IEnumerable<Post>> GetPostsByCategory(string category, int count, int skip = 0)
        {
            bool isAdmin = IsAdmin();

            var posts = _context.Posts
                .Include(p => p.PostCategories)
                .Include(p => p.Comments)
                .Where(p => p.PubDate <= DateTime.UtcNow && (p.IsPublished || isAdmin)
                    && p.PostCategories.Any(pc => string.Equals(pc.CategoryID, category, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(p => p.PubDate)
                .Skip(skip)
                .Take(count)
                .AsEnumerable();

            return Task.FromResult(posts);
        }

        public virtual Task<int> GetPostCount(string category = null)
        {
            bool isAdmin = IsAdmin();

            var count = _context.Posts
                .Where(p => p.PubDate <= DateTime.UtcNow && (p.IsPublished || isAdmin)
                    && (
                        string.IsNullOrEmpty(category)
                        || p.PostCategories.Any(pc => string.Equals(pc.CategoryID, category, StringComparison.OrdinalIgnoreCase)))
                    )
                .Count();

            return Task.FromResult(count);
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
            _context.Entry(post).State = _context.Posts.Any(p => p.ID == post.ID)
                ? EntityState.Modified
                : EntityState.Added;

            // remove old comments, add new comments
            _context.Comments.RemoveRange(_context.Comments.Where(c => !post.Comments.Contains(c)));
            // This next line cannot simply use .Contains per a bug in EF Core that keeps getting punted
            // See: https://github.com/aspnet/EntityFrameworkCore/issues/7687"
            _context.Comments.AddRange(post.Comments
                .Where(c => {
                    foreach (var comment in _context.Comments)
                    {
                        if (comment.Equals(c)) return false;
                    }
                    return true;
                })
            );

            // Remove old post categories and orphaned categories. Add new post categories.
            _context.PostCategories
                .RemoveRange(_context.PostCategories.Where(p => p.PostID == post.ID && !post.PostCategories.Any(pc => p.CategoryID == pc.CategoryID)));
            _context.Categories
                .RemoveRange(_context.Categories.Where(cat => !cat.PostCategories.Any()));
            foreach (var postCat in post.PostCategories)
            {
                if (!_context.Categories.Any(cat => cat.ID == postCat.CategoryID))
                {
                    _context.Add(new Category() { ID = postCat.CategoryID });
                    _context.Entry(postCat).State = EntityState.Added;
                }
                else if (!_context.PostCategories.Any(p => p.CategoryID == postCat.CategoryID && p.PostID == postCat.PostID))
                {
                    _context.Entry(postCat).State = EntityState.Added;
                }
            }

            await _context.SaveChangesAsync();
        }

        protected bool IsAdmin()
        {
            return _contextAccessor.HttpContext?.User?.Identity.IsAuthenticated == true;
        }
    }
}
