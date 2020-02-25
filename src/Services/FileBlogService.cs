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

    /// <summary>
    /// The FileBlogService class. Implements the <see cref="Miniblog.Core.Services.IBlogService" />
    /// </summary>
    /// <seealso cref="Miniblog.Core.Services.IBlogService" />
    public class FileBlogService : IBlogService
    {
        /// <summary>
        /// The files
        /// </summary>
        private const string FILES = "files";

        /// <summary>
        /// The posts
        /// </summary>
        private const string POSTS = "Posts";

        /// <summary>
        /// The cache
        /// </summary>
        private readonly List<Post> cache = new List<Post>();

        /// <summary>
        /// The context accessor
        /// </summary>
        private readonly IHttpContextAccessor contextAccessor;

        /// <summary>
        /// The folder
        /// </summary>
        private readonly string folder;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileBlogService" /> class.
        /// </summary>
        /// <param name="env">The Web host environment.</param>
        /// <param name="contextAccessor">The context accessor.</param>
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

        /// <summary>
        /// Deletes the post.
        /// </summary>
        /// <param name="post">The post.</param>
        /// <returns>Task.</returns>
        public Task DeletePost(Post post)
            bool isAdmin = IsAdmin();
            {
                throw new ArgumentNullException(nameof(post));
            }

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

        /// <summary>
        /// Gets the categories.
        /// </summary>
        /// <returns>Task&lt;IEnumerable&lt;System.String&gt;&gt;.</returns>
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

        /// <summary>
        /// Gets the post by identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Task&lt;Post&gt;.</returns>
        public virtual Task<Post?> GetPostById(string id)
        {
            var isAdmin = this.IsAdmin();
            var post = this.cache.FirstOrDefault(p => p.ID.Equals(id, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(
                post is null || post.PubDate > DateTime.UtcNow || (!post.IsPublished && !isAdmin)
                ? null
                : post);
        }


        /// <summary>
        /// Gets the post by slug.
        /// </summary>
        /// <param name="slug">The slug.</param>
        /// <returns>Task&lt;Post&gt;.</returns>
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

        /// <summary>
        /// Gets the posts.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <param name="skip">The skip.</param>
        /// <returns>Task&lt;IEnumerable&lt;Post&gt;&gt;.</returns>
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

        /// <summary>
        /// Gets the posts by category.
        /// </summary>
        /// <param name="category">The category.</param>
        /// <returns>Task&lt;IEnumerable&lt;Post&gt;&gt;.</returns>
        public virtual IAsyncEnumerable<Post> GetPostsByCategory(string category)
        {
            var isAdmin = this.IsAdmin();

            var posts = from p in this.cache
                        where p.PubDate <= DateTime.UtcNow && (p.IsPublished || isAdmin)
                        where p.Categories.Contains(category, StringComparer.OrdinalIgnoreCase)
                        select p;

            return posts.ToAsyncEnumerable();
        }

        /// <summary>
        /// Saves the file.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="suffix">The suffix.</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
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

        /// <summary>
        /// Saves the post.
        /// </summary>
        /// <param name="post">The post.</param>
        /// <returns>Task.</returns>
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
                                new XElement("comments", string.Empty)
                            ));

            var categories = doc.XPathSelectElement("post/categories");
            foreach (var category in post.Categories)
            {
                categories.Add(new XElement("category", category));
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

        /// <summary>
        /// Determines whether this instance is admin.
        /// </summary>
        /// <returns><c>true</c> if this instance is admin; otherwise, <c>false</c>.</returns>
        protected bool IsAdmin() => this.contextAccessor.HttpContext?.User?.Identity.IsAuthenticated == true;

        /// <summary>
        /// Sorts the cache.
        /// </summary>
        protected void SortCache() => this.cache.Sort((p1, p2) => p2.PubDate.CompareTo(p1.PubDate));

        /// <summary>
        /// Cleans from invalid chars.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>System.String.</returns>
        private static string CleanFromInvalidChars(string input)
        {
            // ToDo: what we are doing here if we switch the blog from windows to unix system or
            // vice versa? we should remove all invalid chars for both systems

            var regexSearch = Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()));
            var r = new Regex($"[{regexSearch}]");
            return r.Replace(input, string.Empty);
        }

        /// <summary>
        /// Formats the date time.
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        /// <returns>System.String.</returns>
        private static string FormatDateTime(DateTime dateTime)
        {
            const string UTC = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'";

            return dateTime.Kind == DateTimeKind.Utc
                ? dateTime.ToString(UTC, CultureInfo.InvariantCulture)
                : dateTime.ToUniversalTime().ToString(UTC, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Loads the categories.
        /// </summary>
        /// <param name="post">The post.</param>
        /// <param name="doc">The document.</param>
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

        /// <summary>
        /// Loads the comments.
        /// </summary>
        /// <param name="post">The post.</param>
        /// <param name="doc">The document.</param>
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

        /// <summary>
        /// Reads the attribute.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="name">The name.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>System.String.</returns>
        private static string ReadAttribute(XElement element, XName name, string defaultValue = "") =>
            element.Attribute(name) is null ? defaultValue : element.Attribute(name)?.Value ?? defaultValue;

        /// <summary>
        /// Reads the value.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="name">The name.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>System.String.</returns>
        private static string ReadValue(XElement doc, XName name, string defaultValue = "") =>
            doc.Element(name) is null ? defaultValue : doc.Element(name)?.Value ?? defaultValue;

        /// <summary>
        /// Gets the file path.
        /// </summary>
        /// <param name="post">The post.</param>
        /// <returns>System.String.</returns>
        [SuppressMessage(
            "Usage",
            "SecurityIntelliSenseCS:MS Security rules violation",
            Justification = "Path not derived from user input.")]
        private string GetFilePath(Post post) => Path.Combine(this.folder, $"{post.ID}.xml");

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        private void Initialize()
        {
            this.LoadPosts();
            this.SortCache();
        }
        
        /// <summary>
        /// Loads the posts.
        /// </summary>
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
                LoadComments(post, doc);
                this.cache.Add(post);
            }
        }
    }
}
