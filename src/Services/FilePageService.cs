using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Miniblog.Core.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Miniblog.Core.Services
{
    public class FilePageService : IPageService
    {
        private const string PAGES = "Pages";
        private const string FILES = "files";

        private readonly List<Page> _cache = new List<Page>();
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly string _folder;

        public FilePageService(IHostingEnvironment env, IHttpContextAccessor contextAccessor)
        {
            _folder = Path.Combine(env.WebRootPath, PAGES);
            _contextAccessor = contextAccessor;

            Initialize();
        }

        public virtual Task<Page> GetIndexPage(string index)
        {
            var page = _cache.FirstOrDefault(p => p.Slug.Equals(index, StringComparison.OrdinalIgnoreCase));
            bool isAdmin = IsAdmin();

            if (page != null && page.PubDate <= DateTime.UtcNow && (page.IsPublished || isAdmin))
            {
                return Task.FromResult(page);
            }

            return Task.FromResult<Page>(null);
        }

        public virtual Task<IEnumerable<Page>> GetPages(int count, int skip = 0)
        {
            bool isAdmin = IsAdmin();

            var pages = _cache
                .Where(p => p.PubDate <= DateTime.UtcNow && (p.IsPublished || isAdmin))
                .Skip(skip)
                .Take(count);

            return Task.FromResult(pages);
        }

        public virtual Task<Page> GetPageBySlug(string slug)
        {
            var page = _cache.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
            bool isAdmin = IsAdmin();

            if (page != null && page.PubDate <= DateTime.UtcNow && (page.IsPublished || isAdmin))
            {
                return Task.FromResult(page);
            }

            return Task.FromResult<Page>(null);
        }

        public virtual Task<Page> GetPageById(string id)
        {
            var page = _cache.FirstOrDefault(p => p.ID.Equals(id, StringComparison.OrdinalIgnoreCase));
            bool isAdmin = IsAdmin();

            if (page != null && page.PubDate <= DateTime.UtcNow && (page.IsPublished || isAdmin))
            {
                return Task.FromResult(page);
            }

            return Task.FromResult<Page>(null);
        }

        public async Task SavePage(Page page)
        {
            string filePath = GetFilePath(page);
            page.LastModified = DateTime.UtcNow;

            XDocument doc = new XDocument(
                            new XElement("page",
                                new XElement("title", page.Title),
                                new XElement("slug", page.Slug),
                                new XElement("pubDate", FormatDateTime(page.PubDate)),
                                new XElement("lastModified", FormatDateTime(page.LastModified)),
                                new XElement("excerpt", page.Excerpt),
                                new XElement("content", page.Content),
                                new XElement("ispublished", page.IsPublished)
                            ));

            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite))
            {
                await doc.SaveAsync(fs, SaveOptions.None, CancellationToken.None).ConfigureAwait(false);
            }

            if (!_cache.Contains(page))
            {
                _cache.Add(page);
                SortCache();
            }
        }

        public Task DeletePage(Page page)
        {
            string filePath = GetFilePath(page);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            if (_cache.Contains(page))
            {
                _cache.Remove(page);
            }

            return Task.CompletedTask;
        }

        public async Task<string> SaveFile(byte[] bytes, string fileName, string suffix = null)
        {
            suffix = CleanFromInvalidChars(suffix ?? DateTime.UtcNow.Ticks.ToString());

            string ext = Path.GetExtension(fileName);
            string name = CleanFromInvalidChars(Path.GetFileNameWithoutExtension(fileName));

            string fileNameWithSuffix = $"{name}_{suffix}{ext}";

            string absolute = Path.Combine(_folder, FILES, fileNameWithSuffix);
            string dir = Path.GetDirectoryName(absolute);

            Directory.CreateDirectory(dir);
            using (var writer = new FileStream(absolute, FileMode.CreateNew))
            {
                await writer.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
            }

            return $"/{PAGES}/{FILES}/{fileNameWithSuffix}";
        }

        private string GetFilePath(Page page)
        {
            return Path.Combine(_folder, page.ID + ".xml");
        }

        private void Initialize()
        {
            LoadPages();
            SortCache();
        }

        private void LoadPages()
        {
            if (!Directory.Exists(_folder))
                Directory.CreateDirectory(_folder);

            // Can this be done in parallel to speed it up?
            foreach (string file in Directory.EnumerateFiles(_folder, "*.xml", SearchOption.TopDirectoryOnly))
            {
                XElement doc = XElement.Load(file);

                Page page = new Page
                {
                    ID = Path.GetFileNameWithoutExtension(file),
                    Title = ReadValue(doc, "title"),
                    Excerpt = ReadValue(doc, "excerpt"),
                    Content = ReadValue(doc, "content"),
                    Slug = ReadValue(doc, "slug").ToLowerInvariant(),
                    PubDate = DateTime.Parse(ReadValue(doc, "pubDate")),
                    LastModified = DateTime.Parse(ReadValue(doc, "lastModified", DateTime.UtcNow.ToString(CultureInfo.InvariantCulture))),
                    IsPublished = bool.Parse(ReadValue(doc, "ispublished", "true")),
                };

                _cache.Add(page);
            }
        }

        private static string ReadValue(XElement doc, XName name, string defaultValue = "")
        {
            if (doc.Element(name) != null)
                return doc.Element(name)?.Value;

            return defaultValue;
        }

        private static string ReadAttribute(XElement element, XName name, string defaultValue = "")
        {
            if (element.Attribute(name) != null)
                return element.Attribute(name)?.Value;

            return defaultValue;
        }

        private static string CleanFromInvalidChars(string input)
        {
            // ToDo: what we are doing here if we switch the blog from windows
            // to unix system or vice versa? we should remove all invalid chars for both systems

            var regexSearch = Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()));
            var r = new Regex($"[{regexSearch}]");
            return r.Replace(input, "");
        }
        
        private static string FormatDateTime(DateTime dateTime)
        {
            const string UTC = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'";

            return dateTime.Kind == DateTimeKind.Utc
                ? dateTime.ToString(UTC)
                : dateTime.ToUniversalTime().ToString(UTC);
        }

        protected void SortCache()
        {
            _cache.Sort((p1, p2) => p2.PubDate.CompareTo(p1.PubDate));
        }

        protected bool IsAdmin()
        {
            return _contextAccessor.HttpContext?.User?.Identity.IsAuthenticated == true;
        }
    }
}