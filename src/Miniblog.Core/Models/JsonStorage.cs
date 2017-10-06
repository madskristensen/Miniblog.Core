using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Miniblog.Core
{
    public class JsonStorage : IBlogStorage
    {
        private IHostingEnvironment _env;
        private string _folder;
        private List<Post> _cache;
        private JsonSerializerSettings _settings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented,
        };

        public JsonStorage(IHostingEnvironment env)
        {
            _env = env;
            _folder = Path.Combine(env.ContentRootPath, "Posts");

            Initialize();
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

        public IEnumerable<Post> GetPosts(int count)
        {
            return _cache.Take(count);
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
            post.LastModified = DateTime.UtcNow;

            string filePath = GetFilePath(post);
            string json = JsonConvert.SerializeObject(post, _settings);

            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite))
            using (var writer = new StreamWriter(fs))
            {
                await writer.WriteAsync(json).ConfigureAwait(false);
            }

            if (!_cache.Contains(post))
            {
                post.ID = Path.GetFileNameWithoutExtension(filePath);
                _cache.Add(post);
                SortCache();
            }
        }

        public string SaveFile(string bits, string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".bin";
            }

            string relative = $"/files/{ Guid.NewGuid()}{extension}";
            string absolute = _env.WebRootFileProvider.GetFileInfo(relative).PhysicalPath;
            string dir = Path.GetDirectoryName(absolute);

            Directory.CreateDirectory(dir);

            byte[] bytes = Convert.FromBase64String(bits);

            File.WriteAllBytes(absolute, bytes);

            return relative;
        }

        private void SortCache()
        {
            _cache.Sort((p1, p2) => p2.PubDate.CompareTo(p1.PubDate));
        }

        private string GetFilePath(Post post)
        {
            return Path.Combine(_folder, post.ID + ".json");
        }

        private void Initialize()
        {
            _cache = new List<Post>();

            foreach (string file in Directory.EnumerateFiles(_folder, "*.json", SearchOption.TopDirectoryOnly))
            {
                string json = File.ReadAllText(file);
                var post = JsonConvert.DeserializeObject<Post>(json);
                post.ID = Path.GetFileNameWithoutExtension(file);

                _cache.Add(post);
            }

            SortCache();
        }
    }
}
