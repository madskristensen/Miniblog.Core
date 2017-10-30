using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Miniblog.Core
{
    public class ImportFileBlogService : IImportBlogService
    {
        public readonly IBlogService _serviceToImportTo;
        public readonly FileBlogService _serviceToImportFrom;

        public ImportFileBlogService(IBlogService service, IHostingEnvironment env, IHttpContextAccessor contextAccessor)
        {
            _serviceToImportTo = service;
            _serviceToImportFrom = new FileBlogService(env, contextAccessor);
        }

        public async Task ImportBlog()
        {
            var postCount = await _serviceToImportFrom.GetPostCount();
            var posts = await _serviceToImportFrom.GetPosts(postCount);

            var tasks = new List<Task>();
            foreach (var post in posts)
            {
                tasks.Add(_serviceToImportTo.SavePost(post));
            }

            await Task.WhenAll(tasks);
        }
    }
}
