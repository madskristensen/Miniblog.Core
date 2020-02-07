using System.Collections.Generic;
using System.Threading.Tasks;
using Miniblog.Core.Models;

namespace Miniblog.Core.Services
{
    public class LiteDatabaseBlogService : IBlogService
    {
        public Task<IEnumerable<Post>> GetPosts()
        {
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<Post>> GetPosts(int count, int skip = 0)
        {
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<Post>> GetPostsByCategory(string category)
        {
            throw new System.NotImplementedException();
        }

        public Task<Post> GetPostBySlug(string slug)
        {
            throw new System.NotImplementedException();
        }

        public Task<Post> GetPostById(string id)
        {
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<string>> GetCategories()
        {
            throw new System.NotImplementedException();
        }

        public Task SavePost(Post post)
        {
            throw new System.NotImplementedException();
        }

        public Task DeletePost(Post post)
        {
            throw new System.NotImplementedException();
        }

        public Task<string> SaveFile(byte[] bytes, string fileName, string suffix = null)
        {
            throw new System.NotImplementedException();
        }
    }
}