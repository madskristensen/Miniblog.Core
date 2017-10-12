using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Miniblog.Core
{
    public interface IBlogStorage
    {
        IEnumerable<Post> GetPosts(int count, int skip = 0);

        IEnumerable<Post> GetPostsByCategory(string category);

        Post GetPostBySlug(string slug);

        Post GetPostById(string id);

        IEnumerable<string> GetCategories();

        Task SavePost(Post post);

        void DeletePost(Post post);

        Task<string> SaveFile(byte[] bytes, string fileName, string suffix = null);
    }

    public abstract class InMemoryBlogStorage : IBlogStorage
    {
        protected List<Post> _cache;

        public IEnumerable<Post> GetPosts(int count, int skip = 0)
        {
            return _cache.Skip(skip).Take(count);
        }

        public IEnumerable<Post> GetPostsByCategory(string category)
        {
            return _cache.Where(p => p.Categories.Contains(category, StringComparer.OrdinalIgnoreCase));
        }

        public Post GetPostBySlug(string slug)
        {
            return _cache.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
        }

        public Post GetPostById(string id)
        {
            return _cache.FirstOrDefault(p => p.ID.Equals(id, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<string> GetCategories()
        {
            return _cache.SelectMany(post => post.Categories)
                         .Select(cat => cat.ToLowerInvariant())
                         .Distinct();
        }

        protected void SortCache()
        {
            _cache.Sort((p1, p2) => p2.PubDate.CompareTo(p1.PubDate));
        }

        public abstract Task SavePost(Post post);

        public abstract void DeletePost(Post post);

        public abstract Task<string> SaveFile(byte[] bytes, string fileName, string suffix = null);
    }
}
