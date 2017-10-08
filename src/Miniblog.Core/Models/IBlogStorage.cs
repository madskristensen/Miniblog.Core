using System.Collections.Generic;
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

        string SaveFile(byte[] bytes, string fileExtension);
    }
}
