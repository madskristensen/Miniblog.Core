using Microsoft.AspNetCore.Hosting;
using System.Xml.Linq;
using System.Xml.XPath;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Miniblog.Core
{
    public class XmlStorage : IBlogStorage
    {
        private IHostingEnvironment _env;
        private string _folder;
        private List<Post> _cache;

        public XmlStorage(IHostingEnvironment env)
        {
            _env = env;
            _folder = Path.Combine(env.WebRootPath, "Posts");

            Initialize();
        }

        public IEnumerable<Post> GetPosts(int count, int skip = 0)
        {
            return _cache.Skip(skip).Take(count);
        }

        public IEnumerable<Post> GetPostsByCategory(string category)
        {
            return _cache.Where(p => p.Categories.Contains(category, StringComparer.OrdinalIgnoreCase));
        }

        public Post GetPostBySlug(string slug)
        {
            return _cache.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
        }

        public Post GetPostById(string id)
        {
            return _cache.FirstOrDefault(p => p.ID.Equals(id, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<string> GetCategories()
        {
            return _cache.SelectMany(post => post.Categories)
                         .Select(cat => cat.ToLowerInvariant())
                         .Distinct();
        }

        public async Task SavePost(Post post)
        {
            string filePath = GetFilePath(post);
            post.LastModified = DateTime.UtcNow;

            XDocument doc = new XDocument(
                            new XElement("post",
                                new XElement("title", post.Title),
                                new XElement("slug", post.Slug),
                                new XElement("pubDate", post.PubDate.ToString("yyyy-MM-dd HH:mm:ss")),
                                new XElement("lastModified", post.LastModified.ToString("yyyy-MM-dd HH:mm:ss")),
                                new XElement("excerpt", post.Excerpt),
                                new XElement("content", post.Content),
                                new XElement("ispublished", post.IsPublished),
                                new XElement("categories", string.Empty),
                                new XElement("comments", string.Empty)
                            ));

            XElement categories = doc.XPathSelectElement("post/categories");
            foreach (string category in post.Categories)
            {
                categories.Add(new XElement("category", category));
            }

            XElement comments = doc.XPathSelectElement("post/comments");
            foreach (Comment comment in post.Comments)
            {
                comments.Add(
                    new XElement("comment",
                        new XElement("author", comment.Author),
                        new XElement("email", comment.Email),
                        new XElement("date", comment.PubDate.ToString("yyyy-MM-dd HH:m:ss")),
                        new XElement("content", comment.Content),
                        new XAttribute("isAdmin", comment.IsAdmin),
                        new XAttribute("id", comment.ID)
                    ));
            }

            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite))
            {
                await doc.SaveAsync(fs, SaveOptions.None, CancellationToken.None);
            }

            if (!_cache.Contains(post))
            {
                _cache.Add(post);
                SortCache();
            }
        }

        public void DeletePost(Post post)
        {
            string filePath = GetFilePath(post);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            if (_cache.Contains(post))
            {
                _cache.Remove(post);
            }
        }

        public string SaveFile(byte[] bytes, string fileName, string suffix = null)
        {
            suffix = suffix ?? DateTime.UtcNow.Ticks.ToString();

            string ext = Path.GetExtension(fileName);
            string name = Path.GetFileNameWithoutExtension(fileName);

            string relative = $"/files/{name}_{suffix}.{ext}";
            string absolute = Path.Combine(_folder, relative);
            string dir = Path.GetDirectoryName(absolute);

            Directory.CreateDirectory(dir);
            File.WriteAllBytes(absolute, bytes);

            return relative;
        }

        private void SortCache()
        {
            _cache.Sort((p1, p2) => p2.PubDate.CompareTo(p1.PubDate));
        }

        private string GetFilePath(Post post)
        {
            return Path.Combine(_folder, post.ID + ".xml");
        }

        private void Initialize()
        {
            _cache = new List<Post>();

            LoadPosts();
            SortCache();
        }

        private void LoadPosts()
        {
            if (!Directory.Exists(_folder))
                Directory.CreateDirectory(_folder);

            // Can this be done in parallel to speed it up?
            foreach (string file in Directory.EnumerateFiles(_folder, "*.xml", SearchOption.TopDirectoryOnly))
            {
                XElement doc = XElement.Load(file);

                Post post = new Post()
                {
                    ID = Path.GetFileNameWithoutExtension(file),
                    Title = ReadValue(doc, "title"),
                    Excerpt = ReadValue(doc, "excerpt"),
                    Content = ReadValue(doc, "content"),
                    Slug = ReadValue(doc, "slug").ToLowerInvariant(),
                    PubDate = DateTime.Parse(ReadValue(doc, "pubDate")),
                    LastModified = DateTime.Parse(ReadValue(doc, "lastModified", DateTime.Now.ToString())),
                    IsPublished = bool.Parse(ReadValue(doc, "ispublished", "true")),
                };

                LoadCategories(post, doc);
                LoadComments(post, doc);
                _cache.Add(post);
            }
        }

        private static void LoadCategories(Post post, XElement doc)
        {
            XElement categories = doc.Element("categories");
            if (categories == null)
                return;

            List<string> list = new List<string>();

            foreach (var node in categories.Elements("category"))
            {
                list.Add(node.Value);
            }

            post.Categories = list.ToArray();
        }

        private static void LoadComments(Post post, XElement doc)
        {
            var comments = doc.Element("comments");

            if (comments == null)
                return;

            foreach (var node in comments.Elements("comment"))
            {
                Comment comment = new Comment()
                {
                    ID = ReadAttribute(node, "id"),
                    Author = ReadValue(node, "author"),
                    Email = ReadValue(node, "email"),
                    IsAdmin = bool.Parse(ReadAttribute(node, "isAdmin", "false")),
                    Content = ReadValue(node, "content").Replace("\n", "<br />"),
                    PubDate = DateTime.Parse(ReadValue(node, "date", "2000-01-01")),
                };

                post.Comments.Add(comment);
            }
        }

        private static string ReadValue(XElement doc, XName name, string defaultValue = "")
        {
            if (doc.Element(name) != null)
                return doc.Element(name).Value;

            return defaultValue;
        }

        private static string ReadAttribute(XElement element, XName name, string defaultValue = "")
        {
            if (element.Attribute(name) != null)
                return element.Attribute(name).Value;

            return defaultValue;
        }
    }
}
