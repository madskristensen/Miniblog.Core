using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Miniblog.Core.Services
{
    public class BaseBlogService
    {
        public readonly string PostDir;

        private const string Posts = "Posts";
        private const string Files = "Files";
        private readonly IHttpContextAccessor _contextAccessor;

        public bool IsAdmin()
        {
            bool? isAdmin = _contextAccessor.HttpContext?.User?.Identity.IsAuthenticated;

            return isAdmin.HasValue && isAdmin.Value;
        }

        public BaseBlogService(IWebHostEnvironment env, IHttpContextAccessor contextAccessor)
        {
            PostDir = Path.Combine(env.WebRootPath, Posts);
            _contextAccessor = contextAccessor;
        }

        public async Task<string> SaveFile(byte[] bytes, string fileName, string suffix = null)
        {
            suffix = CleanFromInvalidChars(suffix ?? DateTime.UtcNow.Ticks.ToString());

            string ext = Path.GetExtension(fileName);
            string name = CleanFromInvalidChars(Path.GetFileNameWithoutExtension(fileName));

            string fileNameWithSuffix = $"{name}_{suffix}{ext}";

            string absolute = Path.Combine(PostDir, Files, fileNameWithSuffix);
            string dir = Path.GetDirectoryName(absolute);

            Directory.CreateDirectory(dir);
            using (var writer = new FileStream(absolute, FileMode.CreateNew))
            {
                await writer.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
            }

            return $"/{Posts}/{Files}/{fileNameWithSuffix}";
        }

        private static string CleanFromInvalidChars(string input)
        {
            // ToDo: what we are doing here if we switch the blog from windows
            // to unix system or vice versa? we should remove all invalid chars for both systems

            var regexSearch = Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()));
            var r = new Regex($"[{regexSearch}]");
            return r.Replace(input, "");
        }
    }
}