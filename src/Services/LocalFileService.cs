using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Miniblog.Core.Services
{
    public class LocalFileService : IFileService
    {
        private readonly string _folder;

        public LocalFileService(IHostingEnvironment env)
        {
            _folder = Path.Combine(env.WebRootPath, "Posts");
        }

        public async Task<string> SaveFile(byte[] bytes, string fileName, string suffix = null)
        {
            suffix = suffix ?? DateTime.UtcNow.Ticks.ToString();

            string ext = Path.GetExtension(fileName);
            string name = Path.GetFileNameWithoutExtension(fileName);

            string relative = $"files/{name}_{suffix}{ext}";
            string absolute = Path.Combine(_folder, relative);
            string dir = Path.GetDirectoryName(absolute);

            Directory.CreateDirectory(dir);
            using (var writer = new FileStream(absolute, FileMode.CreateNew))
            {
                await writer.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
            }

            return "/Posts/" + relative;
        }
    }
}
