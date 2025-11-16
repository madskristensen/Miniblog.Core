using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;

using Miniblog.Core.Models;

namespace Miniblog.Core.Services;

[method: SuppressMessage(
    "Usage",
    "SecurityIntelliSenseCS:MS Security rules violation",
    Justification = "Path not derived from user input.")]
public class FileBlogService(IWebHostEnvironment env, IHttpContextAccessor contextAccessor) : IBlogService
{
    private const string FILES = "files";
    private const string POSTS = "Posts";

    private readonly List<Post> _cache = [];
    private readonly IWebHostEnvironment _env = env ?? throw new ArgumentNullException(nameof(env));
    private bool _initialized = false;

    private string Folder => field ??= Path.Combine(_env.WebRootPath, POSTS);

    public async Task DeletePost(Post post)
    {
        ArgumentNullException.ThrowIfNull(post);

        if (!_initialized)
        {
            await InitializeAsync();
        }

        string filePath = GetFilePath(post);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        if (_cache.Contains(post))
        {
            _ = _cache.Remove(post);
        }
    }

    [SuppressMessage(
        "Globalization",
        "CA1308:Normalize strings to uppercase",
        Justification = "Consumer preference.")]
    public virtual async IAsyncEnumerable<string> GetCategories()
    {
        if (!_initialized)
        {
            await InitializeAsync();
        }

        bool isAdmin = IsAdmin();

        foreach (string? category in _cache
            .Where(p => p.IsPublished || isAdmin)
            .SelectMany(post => post.Categories)
            .Select(cat => cat.ToLowerInvariant())
            .Distinct())
        {
            yield return category;
        }
    }

    public virtual async Task<Post?> GetPostById(string id)
    {
        if (!_initialized)
        {
            await InitializeAsync();
        }

        bool isAdmin = IsAdmin();
        var post = _cache.FirstOrDefault(p => p.ID.Equals(id, StringComparison.OrdinalIgnoreCase));

        return await Task.FromResult(
            post is null || post.PubDate > DateTime.UtcNow || (!post.IsPublished && !isAdmin)
            ? null
            : post);
    }

    public virtual async Task<Post?> GetPostBySlug(string slug)
    {
        if (!_initialized)
        {
            await InitializeAsync();
        }

        bool isAdmin = IsAdmin();
        var post = _cache.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

        return await Task.FromResult(
            post is null || post.PubDate > DateTime.UtcNow || (!post.IsPublished && !isAdmin)
            ? null
            : post);
    }

    /// <remarks>Overload for getPosts method to retrieve all posts.</remarks>
    public virtual async IAsyncEnumerable<Post> GetPosts()
    {
        if (!_initialized)
        {
            await InitializeAsync();
        }

        bool isAdmin = IsAdmin();

        foreach (var post in _cache.Where(p => p.PubDate <= DateTime.UtcNow && (p.IsPublished || isAdmin)))
        {
            yield return post;
        }
    }

    public virtual async IAsyncEnumerable<Post> GetPosts(int count, int skip = 0)
    {
        if (!_initialized)
        {
            await InitializeAsync();
        }

        bool isAdmin = IsAdmin();

        foreach (var post in _cache
            .Where(p => p.PubDate <= DateTime.UtcNow && (p.IsPublished || isAdmin))
            .Skip(skip)
            .Take(count))
        {
            yield return post;
        }
    }

    public virtual async IAsyncEnumerable<Post> GetPostsByCategory(string category)
    {
        if (!_initialized)
        {
            await InitializeAsync();
        }

        bool isAdmin = IsAdmin();

        foreach (var post in _cache
            .Where(p => p.PubDate <= DateTime.UtcNow && (p.IsPublished || isAdmin))
            .Where(p => p.Categories.Contains(category, StringComparer.OrdinalIgnoreCase)))
        {
            yield return post;
        }
    }

    public async IAsyncEnumerable<Post> GetPostsByTag(string tag)
    {
        if (!_initialized)
        {
            await InitializeAsync();
        }

        bool isAdmin = IsAdmin();

        foreach (var post in _cache
                    .Where(p => p.PubDate <= DateTime.UtcNow && (p.IsPublished || isAdmin))
                    .Where(p => p.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)))
        {
            yield return post;
        }
    }

    [SuppressMessage(
        "Globalization",
        "CA1308:Normalize strings to uppercase",
        Justification = "Consumer preference.")]
    public virtual async IAsyncEnumerable<string> GetTags()
    {
        if (!_initialized)
        {
            await InitializeAsync();
        }

        bool isAdmin = IsAdmin();

        foreach (string? tag in _cache
            .Where(p => p.IsPublished || isAdmin)
            .SelectMany(post => post.Tags)
            .Select(tag => tag.ToLowerInvariant())
            .Distinct())
        {
            yield return tag;
        }
    }

    [SuppressMessage(
        "Usage",
        "SecurityIntelliSenseCS:MS Security rules violation",
        Justification = "Caller must review file name.")]
    public async Task<string> SaveFile(byte[] bytes, string fileName, string? suffix = null)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        if (!_initialized)
        {
            await InitializeAsync();
        }

        suffix = CleanFromInvalidChars(suffix ?? DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture));

        string ext = Path.GetExtension(fileName);
        string name = CleanFromInvalidChars(Path.GetFileNameWithoutExtension(fileName));

        string fileNameWithSuffix = $"{name}_{suffix}{ext}";

        string absolute = Path.Combine(Folder, FILES, fileNameWithSuffix);
        string dir = Path.GetDirectoryName(absolute)!;

        _ = Directory.CreateDirectory(dir);
        using FileStream writer = new(absolute, FileMode.CreateNew);
        await writer.WriteAsync(bytes).ConfigureAwait(false);

        return $"/{POSTS}/{FILES}/{fileNameWithSuffix}";
    }

    public async Task SavePost(Post post)
    {
        ArgumentNullException.ThrowIfNull(post);

        if (!_initialized)
        {
            await InitializeAsync();
        }

        string filePath = GetFilePath(post);
        post.LastModified = DateTime.UtcNow;

        XDocument doc = new(
                        new XElement("post",
                            new XElement("title", post.Title),
                            new XElement("slug", post.Slug),
                            new XElement("pubDate", FormatDateTime(post.PubDate)),
                            new XElement("lastModified", FormatDateTime(post.LastModified)),
                            new XElement("excerpt", post.Excerpt),
                            new XElement("content", post.Content),
                            new XElement("ispublished", post.IsPublished),
                            new XElement("categories", string.Empty),
                            new XElement("tags", string.Empty),
                            new XElement("comments", string.Empty)
                        ));

        var categories = doc.XPathSelectElement("post/categories")!;
        foreach (string category in post.Categories)
        {
            categories.Add(new XElement("category", category));
        }

        var tags = doc.XPathSelectElement("post/tags")!;
        foreach (string tag in post.Tags)
        {
            tags.Add(new XElement("tag", tag));
        }

        var comments = doc.XPathSelectElement("post/comments")!;
        foreach (var comment in post.Comments)
        {
            comments.Add(
                new XElement("comment",
                    new XElement("author", comment.Author),
                    new XElement("email", comment.Email),
                    new XElement("date", FormatDateTime(comment.PubDate)),
                    new XElement("content", comment.Content),
                    new XAttribute("isAdmin", comment.IsAdmin),
                    new XAttribute("id", comment.ID)
                ));
        }

        using FileStream fs = new(filePath, FileMode.Create, FileAccess.ReadWrite);
        await doc.SaveAsync(fs, SaveOptions.None, CancellationToken.None).ConfigureAwait(false);

        if (!_cache.Contains(post))
        {
            _cache.Add(post);
            SortCache();
        }
    }

    protected bool IsAdmin() => contextAccessor.HttpContext?.User?.Identity!.IsAuthenticated == true;

    protected void SortCache() => _cache.Sort((p1, p2) => p2.PubDate.CompareTo(p1.PubDate));

    private static string CleanFromInvalidChars(string input)
    {
        // TODO: what we are doing here if we switch the blog from windows to unix system or vice
        // versa? we should remove all invalid chars for both systems

        string regexSearch = Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()));
        Regex r = new($"[{regexSearch}]");
        return r.Replace(input, string.Empty);
    }

    private static string FormatDateTime(DateTime dateTime)
    {
        const string utc = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'";

        return dateTime.Kind == DateTimeKind.Utc
            ? dateTime.ToString(utc, CultureInfo.InvariantCulture)
            : dateTime.ToUniversalTime().ToString(utc, CultureInfo.InvariantCulture);
    }

    private static void LoadCategories(Post post, XElement doc)
    {
        var categories = doc.Element("categories");
        if (categories is null)
        {
            return;
        }

        post.Categories.Clear();
        foreach (string? category in categories.Elements("category").Select(node => node.Value))
        {
            post.Categories.Add(category);
        }
    }

    private static void LoadComments(Post post, XElement doc)
    {
        var comments = doc.Element("comments");

        if (comments is null)
        {
            return;
        }

        foreach (var node in comments.Elements("comment"))
        {
            Comment comment = new()
            {
                ID = ReadAttribute(node, "id"),
                Author = ReadValue(node, "author"),
                Email = ReadValue(node, "email"),
                IsAdmin = bool.Parse(ReadAttribute(node, "isAdmin", "false")),
                Content = ReadValue(node, "content"),
                PubDate = DateTime.Parse(ReadValue(node, "date", "2000-01-01"), CultureInfo.InvariantCulture),
            };

            post.Comments.Add(comment);
        }
    }

    private static void LoadTags(Post post, XElement doc)
    {
        var tags = doc.Element("tags");
        if (tags is null)
        {
            return;
        }

        post.Tags.Clear();
        foreach (string? tag in tags.Elements("tag").Select(node => node.Value))
        {
            post.Tags.Add(tag);
        }
    }

    private static string ReadAttribute(XElement element, XName name, string defaultValue = "") =>
        element.Attribute(name) is null ? defaultValue : element.Attribute(name)?.Value ?? defaultValue;

    private static string ReadValue(XElement doc, XName name, string defaultValue = "") =>
        doc.Element(name) is null ? defaultValue : doc.Element(name)?.Value ?? defaultValue;

    [SuppressMessage(
        "Usage",
        "SecurityIntelliSenseCS:MS Security rules violation",
        Justification = "Path not derived from user input.")]
    private string GetFilePath(Post post) => Path.Combine(Folder, $"{post.ID}.xml");

    private async Task InitializeAsync()
    {
        await LoadPosts();
        SortCache();
        _initialized = true;
    }

    [SuppressMessage(
        "Globalization",
        "CA1308:Normalize strings to uppercase",
        Justification = "The slug should be lower case.")]
    private async Task LoadPosts()
    {
        if (!Directory.Exists(Folder))
        {
            _ = Directory.CreateDirectory(Folder);
        }

        var filePaths = Directory.EnumerateFiles(Folder, "*.xml", SearchOption.TopDirectoryOnly);
        int degreeOfParallelism = Environment.ProcessorCount;

        static async ValueTask<Post> LoadPostFromFile(string filePath, CancellationToken cancellationToken)
        {
            FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var doc = await XElement.LoadAsync(fileStream, LoadOptions.None, CancellationToken.None);
            string id = Path.GetFileNameWithoutExtension(filePath);
            Post post = new()
            {
                ID = Path.GetFileNameWithoutExtension(filePath),
                Title = ReadValue(doc, "title"),
                Excerpt = ReadValue(doc, "excerpt"),
                Content = ReadValue(doc, "content"),
                Slug = ReadValue(doc, "slug").ToLowerInvariant(),
                PubDate = DateTime.Parse(ReadValue(doc, "pubDate"), CultureInfo.InvariantCulture),
                LastModified = DateTime.Parse(
                    ReadValue(
                        doc,
                        "lastModified",
                        DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)),
                    CultureInfo.InvariantCulture),
                IsPublished = bool.Parse(ReadValue(doc, "ispublished", "true")),
            };
            LoadCategories(post, doc);
            LoadTags(post, doc);
            LoadComments(post, doc);
            return post;
        }

        ;
        var posts = filePaths
            .AsParallel()
            .WithDegreeOfParallelism(degreeOfParallelism)
            .ToAsyncEnumerable()
            .Select(LoadPostFromFile);

        await foreach (var post in posts)
        {
            _cache.Add(post);
        }
    }
}
