namespace Miniblog.Core.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Options;

using Miniblog.Core.Models;
using Miniblog.Core.Services;

using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

using WebEssentials.AspNetCore.Pwa;

public partial class BlogController(IBlogService blog, IOptionsSnapshot<BlogSettings> settings, WebManifest manifest) : Controller
{
    [Route("/blog/comment/{postId}")]
    [HttpPost]
    public async Task<IActionResult> AddComment(string postId, Comment comment)
    {
        var post = await blog.GetPostById(postId).ConfigureAwait(true);

        if (!this.ModelState.IsValid)
        {
            return this.View(nameof(Post), post);
        }

        if (post is null || !post.AreCommentsOpen(settings.Value.CommentsCloseAfterDays))
        {
            return this.NotFound();
        }

        ArgumentNullException.ThrowIfNull(comment);

        comment.IsAdmin = this.User.Identity!.IsAuthenticated;
        comment.Content = comment.Content.Trim();
        comment.Author = comment.Author.Trim();
        comment.Email = comment.Email.Trim();

        // the website form key should have been removed by javascript unless the comment was posted
        // by a spam robot
        if (!this.Request.Form.ContainsKey("website"))
        {
            post.Comments.Add(comment);
            await blog.SavePost(post).ConfigureAwait(false);
        }

        return this.Redirect($"{post.GetEncodedLink()}#{comment.ID}");
    }

    [Route("/blog/category/{category}/{page:int?}")]
    [OutputCache(PolicyName = "default")]
    public async Task<IActionResult> Category(string category, int page = 0)
    {
        // get posts for the selected category.
        var posts = blog.GetPostsByCategory(category);

        var totalPostCount = await posts.CountAsync().ConfigureAwait(true);

        // apply paging filter.
        var postsPerPage = settings.Value.PostsPerPage;
        if (postsPerPage <= 0)
        {
            postsPerPage = 4; // Default value if not set or invalid
        }

        var totalPages = (totalPostCount / postsPerPage) - (totalPostCount % postsPerPage == 0 ? 1 : 0);

        var pagedPosts = posts.Skip(postsPerPage * page).Take(postsPerPage);

        // set the view option
        this.ViewData[Constants.ViewOption] = settings.Value.ListView;
        this.ViewData[Constants.PostsPerPage] = postsPerPage;
        this.ViewData[Constants.TotalPages] = totalPages;
        this.ViewData[Constants.TotalPostCount] = totalPostCount;
        this.ViewData[Constants.Title] = $"{manifest.Name} {category}";
        this.ViewData[Constants.Description] = $"Articles posted in the {category} category";
        this.ViewData[Constants.prev] = $"/blog/category/{category}/{page + 1}/";
        this.ViewData[Constants.next] = $"/blog/category/{category}/{(page <= 1 ? null : page - 1 + "/")}";

        return this.View("~/Views/Blog/Index.cshtml", pagedPosts);
    }

    [Route("/blog/comment/{postId}/{commentId}")]
    [Authorize]
    public async Task<IActionResult> DeleteComment(string postId, string commentId)
    {
        var post = await blog.GetPostById(postId).ConfigureAwait(false);
        if (post is null)
        {
            return this.NotFound();
        }

        var comment = post.Comments.FirstOrDefault(c => c.ID.Equals(commentId, StringComparison.OrdinalIgnoreCase));
        if (comment is null)
        {
            return this.NotFound();
        }

        _ = post.Comments.Remove(comment);
        await blog.SavePost(post).ConfigureAwait(false);

        return this.Redirect($"{post.GetEncodedLink()}#comments");
    }

    [Route("/blog/deletepost/{id}")]
    [HttpPost, Authorize, AutoValidateAntiforgeryToken]
    public async Task<IActionResult> DeletePost(string id)
    {
        var existing = await blog.GetPostById(id).ConfigureAwait(false);
        if (existing is null)
        {
            return this.NotFound();
        }

        await blog.DeletePost(existing).ConfigureAwait(false);
        return this.Redirect("/");
    }

    [Route("/blog/edit/{id?}")]
    [HttpGet, Authorize]
    public async Task<IActionResult> Edit(string? id)
    {
        var categories = await blog.GetCategories().ToListAsync();
        categories.Sort();
        this.ViewData[Constants.AllCats] = categories;

        var tags = await blog.GetTags().ToListAsync();
        tags.Sort();
        this.ViewData[Constants.AllTags] = tags;

        if (string.IsNullOrEmpty(id))
        {
            return this.View(new Post());
        }

        var post = await blog.GetPostById(id).ConfigureAwait(false);

        return post is null ? this.NotFound() : this.View(post);
    }

    [Route("/{page:int?}")]
    [OutputCache(PolicyName = "default")]
    public async Task<IActionResult> Index([FromRoute] int page = 0)
    {
        // get published posts.
        var posts = blog.GetPosts();

        var totalPostCount = await posts.CountAsync().ConfigureAwait(true);

        // apply paging filter.
        var postsPerPage = settings.Value.PostsPerPage;
        if (postsPerPage <= 0)
        {
            postsPerPage = 4; // Default value if not set or invalid
        }

        var pagedPosts = posts.Skip(postsPerPage * page).Take(postsPerPage);

        var totalPages = (totalPostCount / postsPerPage) - (totalPostCount % postsPerPage == 0 ? 1 : 0);

        // set the view option
        this.ViewData[Constants.ViewOption] = settings.Value.ListView;
        this.ViewData[Constants.PostsPerPage] = postsPerPage;
        this.ViewData[Constants.TotalPages] = totalPages;
        this.ViewData[Constants.TotalPostCount] = totalPostCount;
        this.ViewData[Constants.Title] = manifest.Name;
        this.ViewData[Constants.Description] = manifest.Description;
        this.ViewData[Constants.prev] = $"/{page + 1}/";
        this.ViewData[Constants.next] = $"/{(page <= 1 ? null : $"{page - 1}/")}";

        return this.View("~/Views/Blog/Index.cshtml", pagedPosts);
    }

    [Route("/blog/{slug?}")]
    [OutputCache(PolicyName = "default")]
    public async Task<IActionResult> Post(string slug)
    {
        var post = await blog.GetPostBySlug(slug).ConfigureAwait(true);

        return post is null ? this.NotFound() : this.View(post);
    }

    /// <summary>
    /// Redirects the old Miniblog URL format to the new blog URL format.
    /// </summary>
    /// <param name="slug">The slug.</param>
    /// <returns>The result.</returns>
    [Route("/post/{slug}")]
    [HttpGet]
    public IActionResult Redirects(string slug) => this.LocalRedirectPermanent($"/blog/{slug}");

    [Route("/blog/tag/{tag}/{page:int?}")]
    [OutputCache(PolicyName = "default")]
    public async Task<IActionResult> Tag(string tag, int page = 0)
    {
        // get posts for the selected tag.
        var posts = blog.GetPostsByTag(tag);

        var totalPostCount = await posts.CountAsync().ConfigureAwait(true);

        // apply paging filter.
        var postsPerPage = settings.Value.PostsPerPage;
        if (postsPerPage <= 0)
        {
            postsPerPage = 4; // Default value if not set or invalid
        }

        var totalPages = (totalPostCount / postsPerPage) - (totalPostCount % postsPerPage == 0 ? 1 : 0);

        var pagedPosts = posts.Skip(postsPerPage * page).Take(postsPerPage);

        // set the view option
        this.ViewData[Constants.ViewOption] = settings.Value.ListView;
        this.ViewData[Constants.PostsPerPage] = postsPerPage;
        this.ViewData[Constants.TotalPages] = totalPages;
        this.ViewData[Constants.TotalPostCount] = totalPostCount;
        this.ViewData[Constants.Title] = $"{manifest.Name} {tag}";
        this.ViewData[Constants.Description] = $"Articles posted in the {tag} tag";
        this.ViewData[Constants.prev] = $"/blog/tag/{tag}/{page + 1}/";
        this.ViewData[Constants.next] = $"/blog/tag/{tag}/{(page <= 1 ? null : $"{page - 1}/")}";

        return this.View("~/Views/Blog/Index.cshtml", pagedPosts);
    }

    [Route("/blog/{slug?}")]
    [HttpPost, Authorize, AutoValidateAntiforgeryToken]
    public async Task<IActionResult> UpdatePost(string? slug, Post post)
    {
        if (!this.ModelState.IsValid)
        {
            return this.View(nameof(Edit), post);
        }

        ArgumentNullException.ThrowIfNull(post);

        var existing = await blog.GetPostById(post.ID).ConfigureAwait(false) ?? post;
        var existingPostWithSameSlug = await blog.GetPostBySlug(existing.Slug).ConfigureAwait(true);
        if (existingPostWithSameSlug is not null && existingPostWithSameSlug.ID != post.ID)
        {
            existing.Slug = Models.Post.CreateSlug(post.Title + DateTime.UtcNow.ToString("yyyyMMddHHmm"), 50);
        }

        string categories = this.Request.Form[Constants.categories]!;
        string tags = this.Request.Form[Constants.tags]!;

        existing.Categories.Clear();
        foreach (var category in categories
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(c => c.Trim().ToLowerInvariant()))
        {
            existing.Categories.Add(category);
        }

        existing.Tags.Clear();
        foreach (var tag in tags
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim().ToLowerInvariant()))
        {
            existing.Tags.Add(tag);
        }

        existing.Title = post.Title.Trim();
        existing.Slug = string.IsNullOrWhiteSpace(post.Slug) ? Models.Post.CreateSlug(post.Title) : post.Slug.Trim();
        existing.IsPublished = post.IsPublished;
        existing.Content = post.Content.Trim();
        existing.Excerpt = post.Excerpt.Trim();

        await this.SaveFilesToDisk(existing).ConfigureAwait(false);

        await blog.SavePost(existing).ConfigureAwait(false);

        return this.Redirect(post.GetEncodedLink());
    }

    [GeneratedRegex("data:[^/]+/(?<ext>[a-z]+);base64,(?<base64>.+)", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex Base64Regex();

    [GeneratedRegex("<img[^>]+ />", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex ImgRegex();

    private async Task SaveFilesToDisk(Post post)
    {
        var imgRegex = ImgRegex();
        var base64Regex = Base64Regex();
        var allowedExtensions = new[] {
          ".jpg",
          ".jpeg",
          ".gif",
          ".png",
          ".webp"
        };

        foreach (Match? match in imgRegex.Matches(post.Content))
        {
            if (match is null)
            {
                continue;
            }

            var doc = new XmlDocument();
            doc.LoadXml($"<root>{match.Value}</root>");

            var img = doc.FirstChild!.FirstChild;
            var srcNode = img!.Attributes!["src"];
            var fileNameNode = img.Attributes["data-filename"];

            // The HTML editor creates base64 DataURIs which we'll have to convert to image files on disk
            if (srcNode is null || fileNameNode is null)
            {
                continue;
            }

            var extension = Path.GetExtension(fileNameNode.Value);

            // Only accept image files
            if (!allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var base64Match = base64Regex.Match(srcNode.Value);
            if (base64Match.Success)
            {
                var bytes = Convert.FromBase64String(base64Match.Groups["base64"].Value);
                srcNode.Value = await blog.SaveFile(bytes, fileNameNode.Value).ConfigureAwait(false);

                _ = img.Attributes.Remove(fileNameNode);
                post.Content = post.Content.Replace(match.Value, img.OuterXml, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
