using System.Threading.Tasks;

namespace Miniblog.Core
{
    public interface IImportBlogService
    {
        /// <summary>
        /// Import all posts in a blog
        /// </summary>
        /// <param name="options">configuration for import</param>
        /// <returns>awaitable task</returns>
        Task ImportBlog();
    }
}
