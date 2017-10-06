using System.Collections.Generic;
using System.Threading.Tasks;

namespace Miniblog.Core
{
    public interface IBlogStorage
    {
        IEnumerable<Post> GetPosts(int count);

        IEnumerable<Post> GetPostsByCategory(string category);

        Post GetPostBySlug(string slug);

        Post GetPostById(string id);

        IEnumerable<string> GetCategories();

        Task SavePost(Post post);

        void DeletePost(Post post);

        string SaveFile(string bits, string fileExtension);
    }
}
