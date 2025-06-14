namespace Miniblog.Core.Services;

using Miniblog.Core.Models;

using System.Collections.Generic;
using System.Threading.Tasks;

public interface IBlogService
{
    public Task DeletePost(Post post);

    public IAsyncEnumerable<string> GetCategories();

    public IAsyncEnumerable<string> GetTags();

    public Task<Post?> GetPostById(string id);

    public Task<Post?> GetPostBySlug(string slug);

    public IAsyncEnumerable<Post> GetPosts();

    public IAsyncEnumerable<Post> GetPosts(int count, int skip = 0);

    public IAsyncEnumerable<Post> GetPostsByCategory(string category);

    public IAsyncEnumerable<Post> GetPostsByTag(string tag);

    public Task<string> SaveFile(byte[] bytes, string fileName, string? suffix = null);

    public Task SavePost(Post post);
}
