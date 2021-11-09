namespace Miniblog.Core.Services
{
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;

    using Miniblog.Core.Models;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using System.Xml.XPath;

    public class FileBlogService : IBlogService
    {
        private const string FILES = "files";

        private const string POSTS = "Posts";

        private readonly List<Post> cache = new List<Post>();

        private readonly IHttpContextAccessor contextAccessor;

        private readonly string folder;

        [SuppressMessage(
                "Usage",
                "SecurityIntelliSenseCS:MS Security rules violation",
                Justification = "Path not derived from user input.")]
        public FileBlogService(IWebHostEnvironment env, IHttpContextAccessor contextAccessor)
        {
            if (env is null)
            {
                throw new ArgumentNullException(nameof(env));
            }

            this.folder = Path.Combine(env.WebRootPath, POSTS);
            this.contextAccessor = contextAccessor;

            this.Initialize();
        }

        public Task DeletePost(Post post)
        {
            if (post is null)
            {
                throw new ArgumentNullException(nameof(post));
            }

            var filePath = this.GetFilePath(post);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            if (this.cache.Contains(post))
            {
                this.cache.Remove(post);
            }

            return Task.CompletedTask;
        }

        [SuppressMessage(
            "Globalization",
            "CA1308:Normalize strings to uppercase",
            Justification = "Consumer preference.")]
        public virtual IAsyncEnumerable<string> GetCategories()
        {
            var isAdmin = this.IsAdmin();

            return this.cache
                .Where(p => p.IsPublished || isAdmin)
                .SelectMany(post => post.Categories)
                .Select(cat => cat.ToLowerInvariant())
                .Distinct()
                .ToAsyncEnumerable();
        }

        [SuppressMessage(
            "Globalization",
            "CA1308:Normalize strings to uppercase",
            Justification = "Consumer preference.")]
        public virtual IAsyncEnumerable<string> GetTags()
        {
            var isAdmin = this.IsAdmin();

            return this.cache
                .Where(p => p.IsPublished || isAdmin)
                .SelectMany(post => post.Tags)
                .Select(tag => tag.ToLowerInvariant())
                .Distinct()
                .ToAsyncEnumerable();
        }

        public virtual Task<Post?> GetPostById(string id)
        {
            var isAdmin = this.IsAdmin();
            var post = this.cache.FirstOrDefault(p => p.ID.Equals(id, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(
                post is null || post.PubDate > DateTime.UtcNow || (!post.IsPublished && !isAdmin)
                ? null
                : post);
        }

        public virtual Task<Post?> GetPostBySlug(string slug)
        {
            var isAdmin = this.IsAdmin();
            var post = this.cache.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(
                post is null || post.PubDate > DateTime.UtcNow || (!post.IsPublished && !isAdmin)
                ? null
                : post);
        }

        /// <remarks>Overload for getPosts method to retrieve all posts.</remarks>
        public virtual IAsyncEnumerable<Post> GetPosts()
        {
            var isAdmin = this.IsAdmin();

            var posts = this.cache
                .Where(p => p.PubDate <= DateTime.UtcNow && (p.IsPublished || isAdmin))
                .ToAsyncEnumerable();

            return posts;
        }

        public virtual IAsyncEnumerable<Post> GetPosts(int count, int skip = 0)
        {
            var isAdmin = this.IsAdmin();

            var posts = this.cache
                .Where(p => p.PubDate <= DateTime.UtcNow && (p.IsPublished || isAdmin))
                .Skip(skip)
                .Take(count)
                .ToAsyncEnumerable();

            return posts;
        }

        public virtual IAsyncEnumerable<Post> GetPostsByCategory(string category)
        {
            var isAdmin = this.IsAdmin();

            var posts = from p in this.cache
                        where p.PubDate <= DateTime.UtcNow && (p.IsPublished || isAdmin)
                        where p.Categories.Contains(category, StringComparer.OrdinalIgnoreCase)
                        select p;

            return posts.ToAsyncEnumerable();
        }

        public IAsyncEnumerable<Post> GetPostsByTag(string tag)
        {
            var isAdmin = this.IsAdmin();

            var posts = from p in this.cache
                        where p.PubDate <= DateTime.UtcNow && (p.IsPublished || isAdmin)
                        where p.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)
                        select p;

            return posts.ToAsyncEnumerable();
        }

        [SuppressMessage(
            "Usage",
            "SecurityIntelliSenseCS:MS Security rules violation",
            Justification = "Caller must review file name.")]
        public async Task<string> SaveFile(byte[] bytes, string fileName, string? suffix = null)
        {
            if (bytes is null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            suffix = CleanFromInvalidChars(suffix ?? DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture));

            var ext = Path.GetExtension(fileName);
            var name = CleanFromInvalidChars(Path.GetFileNameWithoutExtension(fileName));

            var fileNameWithSuffix = $"{name}_{suffix}{ext}";

            var absolute = Path.Combine(this.folder, FILES, fileNameWithSuffix);
            var dir = Path.GetDirectoryName(absolute);

            Directory.CreateDirectory(dir);
            using (var writer = new FileStream(absolute, FileMode.CreateNew))
            {
                await writer.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
            }

            return $"/{POSTS}/{FILES}/{fileNameWithSuffix}";
        }

        public async Task SavePost(Post post)
        {
            if (post is null)
            {
                throw new ArgumentNullException(nameof(post));
            }

            var filePath = this.GetFilePath(post);
            post.LastModified = DateTime.UtcNow;

            var doc = new XDocument(
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

            var categories = doc.XPathSelectElement("post/categories");
            foreach (var category in post.Categories)
            {
                categories.Add(new XElement("category", category));
            }

            var tags = doc.XPathSelectElement("post/tags");
            foreach (var tag in post.Tags)
            {
                tags.Add(new XElement("tag", tag));
            }

            var comments = doc.XPathSelectElement("post/comments");
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

            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite))
            {
                await doc.SaveAsync(fs, SaveOptions.None, CancellationToken.None).ConfigureAwait(false);
            }

            if (!this.cache.Contains(post))
            {
                this.cache.Add(post);
                this.SortCache();
            }
        }

        protected bool IsAdmin() => this.contextAccessor.HttpContext?.User?.Identity.IsAuthenticated == true;

        protected void SortCache() => this.cache.Sort((p1, p2) => p2.PubDate.CompareTo(p1.PubDate));

        private static string CleanFromInvalidChars(string input)
        {
            // ToDo: what we are doing here if we switch the blog from windows to unix system or
            // vice versa? we should remove all invalid chars for both systems

            var regexSearch = Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()));
            var r = new Regex($"[{regexSearch}]");
            return r.Replace(input, string.Empty);
        }

        private static string FormatDateTime(DateTime dateTime)
        {
            const string UTC = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'";

            return dateTime.Kind == DateTimeKind.Utc
                ? dateTime.ToString(UTC, CultureInfo.InvariantCulture)
                : dateTime.ToUniversalTime().ToString(UTC, CultureInfo.InvariantCulture);
        }

        private static void LoadCategories(Post post, XElement doc)
        {
            var categories = doc.Element("categories");
            if (categories is null)
            {
                return;
            }

            post.Categories.Clear();
            categories.Elements("category").Select(node => node.Value).ToList().ForEach(post.Categories.Add);
        }

        private static void LoadTags(Post post, XElement doc)
        {
            var tags = doc.Element("tags");
            if (tags is null)
            {
                return;
            }

            post.Tags.Clear();
            tags.Elements("tag").Select(node => node.Value).ToList().ForEach(post.Tags.Add);
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
                var comment = new Comment
                {
                    ID = ReadAttribute(node, "id"),
                    Author = ReadValue(node, "author"),
                    Email = ReadValue(node, "email"),
                    IsAdmin = bool.Parse(ReadAttribute(node, "isAdmin", "false")),
                    Content = ReadValue(node, "content"),
                    PubDate = DateTime.Parse(ReadValue(node, "date", "2000-01-01"),
                        CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
                };

                post.Comments.Add(comment);
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
        private string GetFilePath(Post post) => Path.Combine(this.folder, $"{post.ID}.xml");

        private void Initialize()
        {
            this.LoadPosts();
            this.SortCache();
        }

        [SuppressMessage(
            "Globalization",
            "CA1308:Normalize strings to uppercase",
            Justification = "The slug should be lower case.")]
        private void LoadPosts()
        {
            if (!Directory.Exists(this.folder))
            {
                Directory.CreateDirectory(this.folder);
            }

            // Can this be done in parallel to speed it up?
            foreach (var file in Directory.EnumerateFiles(this.folder, "*.xml", SearchOption.TopDirectoryOnly))
            {
                var doc = XElement.Load(file);

                var post = new Post
                {
                    ID = Path.GetFileNameWithoutExtension(file),
                    Title = ReadValue(doc, "title"),
                    Excerpt = ReadValue(doc, "excerpt"),
                    Content = ReadValue(doc, "content"),
                    Slug = ReadValue(doc, "slug").ToLowerInvariant(),
                    PubDate = DateTime.Parse(ReadValue(doc, "pubDate"), CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal),
                    LastModified = DateTime.Parse(
                        ReadValue(
                            doc,
                            "lastModified",
                            DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)),
                        CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
                    IsPublished = bool.Parse(ReadValue(doc, "ispublished", "true")),
                };

                LoadCategories(post, doc);
                LoadTags(post, doc);
                LoadComments(post, doc);
                this.cache.Add(post);
            }
        }
    }
}
