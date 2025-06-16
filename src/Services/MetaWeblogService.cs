namespace Miniblog.Core.Services;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using WilderMinds.MetaWeblog;

public class MetaWeblogService(
    IBlogService blog,
    IConfiguration config,
    IHttpContextAccessor context,
    IUserServices userServices)
    : IMetaWeblogProvider
{
    public Task<int> AddCategoryAsync(string key, string username, string password, NewCategory category)
    {
        this.ValidateUser(username, password);

        throw new NotImplementedException();
    }

    public Task<string> AddPageAsync(string blogid, string username, string password, Page page, bool publish)
    {
        this.ValidateUser(username, password);

        throw new NotImplementedException();
    }

    public async Task<string> AddPostAsync(string blogid, string username, string password, Post post, bool publish)
    {
        this.ValidateUser(username, password);

        ArgumentNullException.ThrowIfNull(post);

        var newPost = new Models.Post
        {
            Title = post.title,
            Slug = !string.IsNullOrWhiteSpace(post.wp_slug) ? post.wp_slug : Models.Post.CreateSlug(post.title),
            Excerpt = post.mt_excerpt,
            Content = post.description,
            IsPublished = publish
        };

        post.categories.ToList().ForEach(newPost.Categories.Add);
        post.mt_keywords.Split(',').ToList().ForEach(newPost.Tags.Add);

        if (post.dateCreated != DateTime.MinValue)
        {
            newPost.PubDate = post.dateCreated;
        }

        await blog.SavePost(newPost).ConfigureAwait(false);

        return newPost.ID;
    }

    public Task<bool> DeletePageAsync(string blogid, string username, string password, string pageid)
    {
        this.ValidateUser(username, password);

        throw new NotImplementedException();
    }

    public async Task<bool> DeletePostAsync(string key, string postid, string username, string password, bool publish)
    {
        this.ValidateUser(username, password);

        var post = await blog.GetPostById(postid).ConfigureAwait(false);
        if (post is null)
        {
            return false;
        }

        await blog.DeletePost(post).ConfigureAwait(false);
        return true;
    }

    public Task<bool> EditPageAsync(string blogid, string pageid, string username, string password, Page page, bool publish)
    {
        this.ValidateUser(username, password);

        throw new NotImplementedException();
    }

    public async Task<bool> EditPostAsync(string postid, string username, string password, Post post, bool publish)
    {
        this.ValidateUser(username, password);

        var existing = await blog.GetPostById(postid).ConfigureAwait(false);

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
        existing.Tags.Clear();
        post.mt_keywords.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList().ForEach(existing.Tags.Add);

        if (post.dateCreated != DateTime.MinValue)
        {
            existing.PubDate = post.dateCreated;
        }

        await blog.SavePost(existing).ConfigureAwait(false);

        return true;
    }

    public Task<Author[]> GetAuthorsAsync(string blogid, string username, string password) =>
        throw new NotImplementedException();

    public async Task<CategoryInfo[]> GetCategoriesAsync(string blogid, string username, string password)
    {
        this.ValidateUser(username, password);

        return await blog.GetCategories()
            .Select(
                cat =>
                    new CategoryInfo
                    {
                        categoryid = cat,
                        title = cat
                    })
            .ToArrayAsync();
    }

    public Task<Page> GetPageAsync(string blogid, string pageid, string username, string password) =>
        throw new NotImplementedException();

    public Task<Page[]> GetPagesAsync(string blogid, string username, string password, int numPages) =>
        throw new NotImplementedException();

    public async Task<Post?> GetPostAsync(string postid, string username, string password)
    {
        this.ValidateUser(username, password);

        var post = await blog.GetPostById(postid).ConfigureAwait(false);

        return post is null ? null : this.ToMetaWebLogPost(post);
    }

    public async Task<Post[]> GetRecentPostsAsync(string blogid, string username, string password, int numberOfPosts)
    {
        this.ValidateUser(username, password);

        return await blog.GetPosts(numberOfPosts)
            .Select(this.ToMetaWebLogPost)
            .ToArrayAsync();
    }

    public async Task<Tag[]> GetTagsAsync(string blogid, string username, string password)
    {
        this.ValidateUser(username, password);

        return await blog.GetTags()
            .Select(
                tag =>
                    new Tag
                    {
                        name = tag
                    })
            .ToArrayAsync();
    }

    public Task<UserInfo> GetUserInfoAsync(string key, string username, string password)
    {
        this.ValidateUser(username, password);

        throw new NotImplementedException();
    }

    public Task<BlogInfo[]> GetUsersBlogsAsync(string key, string username, string password)
    {
        this.ValidateUser(username, password);

        var request = context.HttpContext!.Request;
        var url = $"{request.Scheme}://{request.Host}";

        return Task.FromResult(
            new[]
            {
                new BlogInfo
                {
                    blogid ="1",
                    blogName = config[Constants.Config.Blog.Name] ?? nameof(MetaWeblogService),
                    url = url
                }
            });
    }

    public async Task<MediaObjectInfo> NewMediaObjectAsync(string blogid, string username, string password, MediaObject mediaObject)
    {
        this.ValidateUser(username, password);

        ArgumentNullException.ThrowIfNull(mediaObject);

        var bytes = Convert.FromBase64String(mediaObject.bits);
        var path = await blog.SaveFile(bytes, mediaObject.name).ConfigureAwait(false);

        return new MediaObjectInfo { url = path };
    }

    private Post ToMetaWebLogPost(Models.Post post)
    {
        var request = context.HttpContext!.Request;
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
            categories = post.Categories.ToArray(),
            mt_keywords = string.Join(',', post.Tags)
        };
    }

    private void ValidateUser(string username, string password)
    {
        if (userServices.ValidateUser(username, password) == false)
        {
            throw new MetaWeblogException(Properties.Resources.Unauthorized);
        }

        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
        identity.AddClaim(new Claim(ClaimTypes.Name, username));

        context.HttpContext!.User = new ClaimsPrincipal(identity);
    }
}
