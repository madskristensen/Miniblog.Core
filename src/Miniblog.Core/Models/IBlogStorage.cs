using System.Collections.Generic;
using System.Threading.Tasks;

namespace Miniblog.Core
{
    public interface IBlogStorage
    {
        IEnumerable<Post> GetPosts(int count);

        Post GetPostBySlug(string slug);

        Post GetPostById(string id);

        Task Save(Post post);

        void Delete(Post post);
    }
}
