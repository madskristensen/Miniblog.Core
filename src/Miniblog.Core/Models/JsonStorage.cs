using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Miniblog.Core
{
    public class JsonStorage : InMemoryBlogStorage
    {
        private IHostingEnvironment _env;
        private string _folder;
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

        public override async Task SavePost(Post post)
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
                _cache.Add(post);
                SortCache();
            }
        }

        public override void DeletePost(Post post)
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

        public async override Task<string> SaveFile(byte[] bytes, string fileName, string suffix = null)
        {
            suffix = suffix ?? DateTime.UtcNow.Ticks.ToString();

            string ext = Path.GetExtension(fileName);
            string name = Path.GetFileNameWithoutExtension(fileName);

            string relative = $"/files/{name}_{suffix}{ext}";
            string absolute = _env.WebRootFileProvider.GetFileInfo(relative).PhysicalPath;
            string dir = Path.GetDirectoryName(absolute);

            Directory.CreateDirectory(dir);

            using (var writer = new FileStream(absolute, FileMode.CreateNew))
            {
                await writer.WriteAsync(bytes, 0, bytes.Length);
            }

            return relative;
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
