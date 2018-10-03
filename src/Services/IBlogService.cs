using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Miniblog.Core.Models;

namespace Miniblog.Core.Services
{
    public interface IBlogService
    {
        Task<IEnumerable<Post>> GetPosts(int count, int skip = 0);

        Task<IEnumerable<Post>> GetPostsByCategory(string category);

        Task<IEnumerable<Post>> GetPostsByDate(DateTime date);

        Task<IEnumerable<Post>> GetPostsByMonth(DateTime date);

        Task<IEnumerable<Post>> GetPostsByYear(DateTime date);

        Task<IEnumerable<Post>> GetPostsByTimeSpan(DateTime firstDay, DateTime lastDay);

        Task<IEnumerable<Post>> GetPostsBySearch(string searchTerm);

        Task<Post> GetFirstPost();

        Task<Post> GetPostBySlug(string slug);

        Task<Post> GetPostById(string id);

        Task<IEnumerable<string>> GetCategories();

        Task<IOrderedEnumerable<CategoryCount>> GetCategoriesCount();

        Task SavePost(Post post);

        Task DeletePost(Post post);

        Task<string> SaveFile(byte[] bytes, string fileName, string suffix = null);
    }
}