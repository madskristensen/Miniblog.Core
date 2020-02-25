namespace Miniblog.Core.Services
{
    using Microsoft.AspNetCore.Authentication.Cookies;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;

    using System;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;

    using WilderMinds.MetaWeblog;

    /// <summary>
    /// The MetaWeblogService class. Implements the <see
    /// cref="IMetaWeblogProvider" />
    /// </summary>
    /// <seealso cref="IMetaWeblogProvider" />
    public class MetaWeblogService : IMetaWeblogProvider
    {
        /// <summary>
        /// The blog
        /// </summary>
        private readonly IBlogService blog;

        /// <summary>
        /// The configuration
        /// </summary>
        private readonly IConfiguration config;

        /// <summary>
        /// The context
        /// </summary>
        private readonly IHttpContextAccessor context;

        /// <summary>
        /// The user services
        /// </summary>
        private readonly IUserServices userServices;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetaWeblogService" /> class.
        /// </summary>
        /// <param name="blog">The blog.</param>
        /// <param name="config">The configuration.</param>
        /// <param name="context">The context.</param>
        /// <param name="userServices">The user services.</param>
        public MetaWeblogService(
            IBlogService blog,
            IConfiguration config,
            IHttpContextAccessor context,
            IUserServices userServices)
        {
            this.blog = blog;
            this.config = config;
            this.userServices = userServices;
            this.context = context;
        }

        /// <summary>
        /// Adds the category.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="category">The category.</param>
        /// <returns>Task&lt;System.Int32&gt;.</returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task<int> AddCategoryAsync(string key, string username, string password, NewCategory category)
        {
            this.ValidateUser(username, password);

            throw new NotImplementedException();
        }

        /// <summary>
        /// Adds the page.
        /// </summary>
        /// <param name="blogid">The blog identifier.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="page">The page.</param>
        /// <param name="publish">if set to <c>true</c> [publish].</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task<string> AddPageAsync(string blogid, string username, string password, Page page, bool publish)
        {
            this.ValidateUser(username, password);

            throw new NotImplementedException();
        }

        /// <summary>
        /// Adds the post.
        /// </summary>
        /// <param name="blogid">The blog identifier.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="post">The post.</param>
        /// <param name="publish">if set to <c>true</c> [publish].</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        public async Task<string> AddPostAsync(string blogid, string username, string password, Post post, bool publish)
        {
            this.ValidateUser(username, password);

            if (post is null)
            {
                throw new ArgumentNullException(nameof(post));
            }

            var newPost = new Models.Post
            {
                Title = post.title,
                Slug = !string.IsNullOrWhiteSpace(post.wp_slug) ? post.wp_slug : Models.Post.CreateSlug(post.title),
                Excerpt = post.mt_excerpt,
                Content = post.description,
                IsPublished = publish
            };

            post.categories.ToList().ForEach(newPost.Categories.Add);

            if (post.dateCreated != DateTime.MinValue)
            {
                newPost.PubDate = post.dateCreated;
            }

            await this.blog.SavePost(newPost).ConfigureAwait(false);

            return newPost.ID;
        }

        /// <summary>
        /// Deletes the page.
        /// </summary>
        /// <param name="blogid">The blog identifier.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="pageid">The page identifier.</param>
        /// <returns>Task&lt;System.Boolean&gt;.</returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task<bool> DeletePageAsync(string blogid, string username, string password, string pageid)
        {
            this.ValidateUser(username, password);

            throw new NotImplementedException();
        }

        /// <summary>
        /// Deletes the post.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="postid">The post identifier.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="publish">if set to <c>true</c> [publish].</param>
        /// <returns>Task&lt;System.Boolean&gt;.</returns>
        public async Task<bool> DeletePostAsync(string key, string postid, string username, string password, bool publish)
        {
            this.ValidateUser(username, password);

            var post = await this.blog.GetPostById(postid).ConfigureAwait(false);
            if (post is null)
            {
                return false;
            }

            await this.blog.DeletePost(post).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// Edits the page.
        /// </summary>
        /// <param name="blogid">The blog identifier.</param>
        /// <param name="pageid">The page identifier.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="page">The page.</param>
        /// <param name="publish">if set to <c>true</c> [publish].</param>
        /// <returns>Task&lt;System.Boolean&gt;.</returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task<bool> EditPageAsync(string blogid, string pageid, string username, string password, Page page, bool publish)
        {
            this.ValidateUser(username, password);

            throw new NotImplementedException();
        }

        /// <summary>
        /// Edits the post.
        /// </summary>
        /// <param name="postid">The post identifier.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="post">The post.</param>
        /// <param name="publish">if set to <c>true</c> [publish].</param>
        /// <returns>Task&lt;System.Boolean&gt;.</returns>
        public async Task<bool> EditPostAsync(string postid, string username, string password, Post post, bool publish)
        {
            this.ValidateUser(username, password);

            var existing = await this.blog.GetPostById(postid).ConfigureAwait(false);

            if (existing is null || post is null)
            {
                return false;
            }

            existing.Title = post.title;
            existing.Slug = post.wp_slug;
            existing.Excerpt = post.mt_excerpt;
            existing.Content = post.description;
            existing.IsPublished = publish;
            existing.Categories.Clear();
            post.categories.ToList().ForEach(existing.Categories.Add);

            if (post.dateCreated != DateTime.MinValue)
            {
                existing.PubDate = post.dateCreated;
            }

            await this.blog.SavePost(existing).ConfigureAwait(false);

            return true;
        }

        /// <summary>
        /// Gets the authors.
        /// </summary>
        /// <param name="blogid">The blog identifier.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>Task&lt;Author[]&gt;.</returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task<Author[]> GetAuthorsAsync(string blogid, string username, string password) =>
            throw new NotImplementedException();

        /// <summary>
        /// Gets the categories.
        /// </summary>
        /// <param name="blogid">The blog identifier.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>Task&lt;CategoryInfo[]&gt;.</returns>
        public async Task<CategoryInfo[]> GetCategoriesAsync(string blogid, string username, string password)
        {
            this.ValidateUser(username, password);

            return await this.blog.GetCategories()
                .Select(
                    cat =>
                        new CategoryInfo
                        {
                            categoryid = cat,
                            title = cat
                        })
                .ToArrayAsync();
        }

        /// <summary>
        /// Gets the page.
        /// </summary>
        /// <param name="blogid">The blog identifier.</param>
        /// <param name="pageid">The page identifier.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>Task&lt;Page&gt;.</returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task<Page> GetPageAsync(string blogid, string pageid, string username, string password) =>
            throw new NotImplementedException();

        /// <summary>
        /// Gets the pages.
        /// </summary>
        /// <param name="blogid">The blog identifier.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="numPages">The number pages.</param>
        /// <returns>Task&lt;Page[]&gt;.</returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task<Page[]> GetPagesAsync(string blogid, string username, string password, int numPages) =>
            throw new NotImplementedException();

        /// <summary>
        /// Gets the specified post for the user.
        /// </summary>
        /// <param name="postid">The post identifier.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>Task&lt;Post&gt;.</returns>
        public async Task<Post?> GetPostAsync(string postid, string username, string password)
        {
            this.ValidateUser(username, password);

            var post = await this.blog.GetPostById(postid).ConfigureAwait(false);

            return post is null ? null : this.ToMetaWebLogPost(post);
        }

        /// <summary>
        /// Gets the recent posts for the specified user.
        /// </summary>
        /// <param name="blogid">The blog identifier.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="numberOfPosts">The number of posts.</param>
        /// <returns>Task&lt;Post[]&gt;.</returns>
        public async Task<Post[]> GetRecentPostsAsync(string blogid, string username, string password, int numberOfPosts)
        {
            this.ValidateUser(username, password);

            return await this.blog.GetPosts(numberOfPosts)
                .Select(this.ToMetaWebLogPost)
                .ToArrayAsync();
        }

        /// <summary>
        /// Gets the specified user information.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>Task&lt;UserInfo&gt;.</returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task<UserInfo> GetUserInfoAsync(string key, string username, string password)
        {
            this.ValidateUser(username, password);

            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the specified user's blogs.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>Task&lt;BlogInfo[]&gt;.</returns>
        public Task<BlogInfo[]> GetUsersBlogsAsync(string key, string username, string password)
        {
            this.ValidateUser(username, password);

            var request = this.context.HttpContext.Request;
            var url = $"{request.Scheme}://{request.Host}";

            return Task.FromResult(
                new[]
                {
                    new BlogInfo
                    {
                        blogid ="1",
                        blogName = this.config[Constants.Config.Blog.Name] ?? nameof(MetaWeblogService),
                        url = url
                    }
                });
        }

        /// <summary>
        /// Creates a new media object.
        /// </summary>
        /// <param name="blogid">The blog identifier.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="mediaObject">The media object.</param>
        /// <returns>Task&lt;MediaObjectInfo&gt;.</returns>
        public async Task<MediaObjectInfo> NewMediaObjectAsync(string blogid, string username, string password, MediaObject mediaObject)
        {
            this.ValidateUser(username, password);

            if (mediaObject is null)
            {
                throw new ArgumentNullException(nameof(mediaObject));
            }

            var bytes = Convert.FromBase64String(mediaObject.bits);
            var path = await this.blog.SaveFile(bytes, mediaObject.name).ConfigureAwait(false);

            return new MediaObjectInfo { url = path };
        }

        /// <summary>
        /// Converts to metaweblogpost.
        /// </summary>
        /// <param name="post">The post.</param>
        /// <returns>WilderMinds.MetaWeblog.Post.</returns>
        private Post ToMetaWebLogPost(Models.Post post)
        {
            var request = this.context.HttpContext.Request;
            var url = $"{request.Scheme}://{request.Host}";

            return new Post
            {
                postid = post.ID,
                title = post.Title,
                wp_slug = post.Slug,
                permalink = url + post.GetLink(),
                dateCreated = post.PubDate,
                mt_excerpt = post.Excerpt,
                description = post.Content,
                categories = post.Categories.ToArray()
            };
        }

        /// <summary>
        /// Validates the user.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <exception cref="MetaWeblogException">Unauthorized</exception>
        private void ValidateUser(string username, string password)
        {
            if (this.userServices.ValidateUser(username, password) == false)
            {
                throw new MetaWeblogException(Properties.Resources.Unauthorized);
            }

            var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
            identity.AddClaim(new Claim(ClaimTypes.Name, username));

            this.context.HttpContext.User = new ClaimsPrincipal(identity);
        }
    }
}
