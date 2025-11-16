using Miniblog.Core.Models;

namespace Miniblog.Core.Services;

public abstract class InMemoryBlogServiceBase(IHttpContextAccessor contextAccessor) : IBlogService
{
    protected List<Post> Cache { get; } = [];

    protected IHttpContextAccessor ContextAccessor { get; } = contextAccessor;

    public abstract Task DeletePost(Post post);

    [SuppressMessage(
        "Globalization",
        "CA1308:Normalize strings to uppercase",
        Justification = "Consumer preference.")]
    public virtual IAsyncEnumerable<string> GetCategories()
    {
        bool isAdmin = IsAdmin();

        var categories = Cache
            .Where(p => p.IsPublished || isAdmin)
            .SelectMany(post => post.Categories)
            .Select(cat => cat.ToLowerInvariant())
            .Distinct()
            .ToAsyncEnumerable();

        return categories;
    }

    public virtual Task<Post?> GetPostById(string id)
    {
        bool isAdmin = IsAdmin();
        var post = Cache.FirstOrDefault(p => p.ID.Equals(id, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(
            post is null || !post.IsVisible() || !isAdmin
            ? null
            : post);
    }

    public virtual Task<Post?> GetPostBySlug(string slug)
    {
        bool isAdmin = IsAdmin();
        var post = Cache.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(
            post is null || !post.IsVisible() || !isAdmin
            ? null
            : post);
    }

    /// <remarks>Overload for getPosts method to retrieve all posts.</remarks>
    public virtual IAsyncEnumerable<Post> GetPosts()
    {
        bool isAdmin = IsAdmin();
        return Cache.Where(p => p.IsVisible() || isAdmin).ToAsyncEnumerable();
    }

    public virtual IAsyncEnumerable<Post> GetPosts(int count, int skip = 0)
    {
        bool isAdmin = IsAdmin();

        var posts = Cache
            .Where(p => p.IsVisible() || isAdmin)
            .Skip(skip)
            .Take(count)
            .ToAsyncEnumerable();

        return posts;
    }

    public virtual IAsyncEnumerable<Post> GetPostsByCategory(string category)
    {
        bool isAdmin = IsAdmin();

        var posts = from p in Cache
                    where p.IsVisible() || isAdmin
                    where p.Categories.Contains(category, StringComparer.OrdinalIgnoreCase)
                    select p;

        return posts.ToAsyncEnumerable();
    }

    public virtual IAsyncEnumerable<Post> GetPostsByTag(string tag)
    {
        bool isAdmin = IsAdmin();

        var posts = from p in Cache
                    where p.IsVisible() || isAdmin
                    where p.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)
                    select p;

        return posts.ToAsyncEnumerable();
    }

    [SuppressMessage(
        "Globalization",
        "CA1308:Normalize strings to uppercase",
        Justification = "Consumer preference.")]
    public virtual IAsyncEnumerable<string> GetTags()
    {
        bool isAdmin = IsAdmin();

        var tags = Cache
            .Where(p => p.IsPublished || isAdmin)
            .SelectMany(post => post.Tags)
            .Select(tag => tag.ToLowerInvariant())
            .Distinct()
            .ToAsyncEnumerable();

        return tags;
    }

    public abstract Task<string> SaveFile(byte[] bytes, string fileName, string? suffix = null);

    public abstract Task SavePost(Post post);

    protected bool IsAdmin() => ContextAccessor.HttpContext?.User?.Identity!.IsAuthenticated ?? false;

    protected void SortCache() => Cache.Sort((p1, p2) => p2.PubDate.CompareTo(p1.PubDate));
}
