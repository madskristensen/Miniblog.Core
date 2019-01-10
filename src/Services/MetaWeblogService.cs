using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using WilderMinds.MetaWeblog;

namespace Miniblog.Core.Services
{
    public class MetaWeblogService : IMetaWeblogProvider
    {
        private readonly IBlogService _blog;
        private readonly IConfiguration _config;
        private readonly IUserServices _userServices;
        private readonly IHttpContextAccessor _context;

        public MetaWeblogService(IBlogService blog, IConfiguration config, IHttpContextAccessor context, IUserServices userServices)
        {
            _blog = blog;
            _config = config;
            _userServices = userServices;
            _context = context;
        }

        public async Task<string> AddPostAsync(string blogid, string username, string password, WilderMinds.MetaWeblog.Post post, bool publish)
        {
            ValidateUser(username, password);

            var newPost = new Models.Post
            {
                Title = post.title,
                Slug = !string.IsNullOrWhiteSpace(post.wp_slug) ? post.wp_slug : Models.Post.CreateSlug(post.title),
                Content = post.description,
                IsPublished = publish,
                Categories = post.categories,
            };

            if (post.dateCreated != DateTime.MinValue)
            {
                newPost.PubDate = post.dateCreated;
            }

            await _blog.SavePost(newPost);

            return newPost.ID;
        }

        public async Task<bool> DeletePostAsync(string key, string postid, string username, string password, bool publish)
        {
            ValidateUser(username, password);

            var post = await _blog.GetPostById(postid);

            if (post != null)
            {
                await _blog.DeletePost(post);
                return true;
            }

            return false;
        }

        public async Task<bool> EditPostAsync(string postid, string username, string password, WilderMinds.MetaWeblog.Post post, bool publish)
        {
            ValidateUser(username, password);

            var existing = await _blog.GetPostById(postid);

            if (existing != null)
            {
                existing.Title = post.title;
                existing.Slug = post.wp_slug;
                existing.Content = post.description;
                existing.IsPublished = publish;
                existing.Categories = post.categories;

                if (post.dateCreated != DateTime.MinValue)
                {
                    existing.PubDate = post.dateCreated;
                }

                await _blog.SavePost(existing);

                return true;
            }

            return false;
        }

        public async Task<CategoryInfo[]> GetCategoriesAsync(string blogid, string username, string password)
        {
            ValidateUser(username, password);

            var categories = await _blog.GetCategories();
            return categories.Select(cat =>
                    new CategoryInfo
                    {
                        categoryid = cat,
                        title = cat,
                    })
                .ToArray();
        }

        public async Task<WilderMinds.MetaWeblog.Post> GetPostAsync(string postid, string username, string password)
        {
            ValidateUser(username, password);

            var post = await _blog.GetPostById(postid);

            if (post != null)
            {
                return ToMetaWebLogPost(post);
            }

            return null;
        }

        public async Task<WilderMinds.MetaWeblog.Post[]> GetRecentPostsAsync(string blogid, string username, string password, int numberOfPosts)
        {
            ValidateUser(username, password);

            var posts = await _blog.GetPosts(numberOfPosts);

            return posts.Select(ToMetaWebLogPost).ToArray();
        }

        public Task<BlogInfo[]> GetUsersBlogsAsync(string key, string username, string password)
        {
            ValidateUser(username, password);

            var request = _context.HttpContext.Request;
            var url = $"{request.Scheme}://{request.Host}";

            return Task.FromResult(new[]
            {
                new BlogInfo
                {
                    blogid = "1",
                    blogName = _config["blog:name"] ?? nameof(MetaWeblogService),
                    url = url,
                },
            });
        }

        public async Task<MediaObjectInfo> NewMediaObjectAsync(string blogid, string username, string password, MediaObject mediaObject)
        {
            ValidateUser(username, password);
            var bytes = Convert.FromBase64String(mediaObject.bits);
            var path = await _blog.SaveFile(bytes, mediaObject.name);

            return new MediaObjectInfo { url = path };
        }

        public Task<UserInfo> GetUserInfoAsync(string key, string username, string password)
        {
            ValidateUser(username, password);

            throw new NotImplementedException();
        }

        public Task<int> AddCategoryAsync(string key, string username, string password, NewCategory category)
        {
            ValidateUser(username, password);
            throw new NotImplementedException();
        }

        public Task<Page> GetPageAsync(string blogid, string pageid, string username, string password)
        {
            throw new NotImplementedException();
        }

        public Task<Page[]> GetPagesAsync(string blogid, string username, string password, int numPages)
        {
            throw new NotImplementedException();
        }

        public Task<Author[]> GetAuthorsAsync(string blogid, string username, string password)
        {
            throw new NotImplementedException();
        }

        public Task<string> AddPageAsync(string blogid, string username, string password, Page page, bool publish)
        {
            throw new NotImplementedException();
        }

        public Task<bool> EditPageAsync(string blogid, string pageid, string username, string password, Page page, bool publish)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeletePageAsync(string blogid, string username, string password, string pageid)
        {
            throw new NotImplementedException();
        }

        private void ValidateUser(string username, string password)
        {
            if (_userServices.ValidateUser(username, password) == false)
            {
                throw new MetaWeblogException("Unauthorized");
            }

            var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
            identity.AddClaim(new Claim(ClaimTypes.Name, username));

            _context.HttpContext.User = new ClaimsPrincipal(identity);
        }

        private WilderMinds.MetaWeblog.Post ToMetaWebLogPost(Models.Post post)
        {
            var request = _context.HttpContext.Request;
            var url = $"{request.Scheme}://{request.Host}";

            return new WilderMinds.MetaWeblog.Post
            {
                postid = post.ID,
                title = post.Title,
                wp_slug = post.Slug,
                permalink = url + post.GetLink(),
                dateCreated = post.PubDate,
                description = post.Content,
                categories = post.Categories.ToArray(),
            };
        }
    }
}
