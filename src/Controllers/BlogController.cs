using System.Text.RegularExpressions;
using System.Xml;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Options;

using Miniblog.Core.Models;
using Miniblog.Core.Services;

using WebEssentials.AspNetCore.Pwa;

namespace Miniblog.Core.Controllers;

public partial class BlogController(IBlogService blog, IOptionsSnapshot<BlogSettings> settings, WebManifest manifest) : Controller
{
    [Route("/blog/comment/{postId}")]
    [HttpPost]
    public async Task<IActionResult> AddComment(string postId, Comment comment)
    {
        var post = await blog.GetPostById(postId).ConfigureAwait(true);

        if (!ModelState.IsValid)
        {
            return View(nameof(Post), post);
        }

        if (post is null || !post.AreCommentsOpen(settings.Value.CommentsCloseAfterDays))
        {
            return NotFound();
        }

        ArgumentNullException.ThrowIfNull(comment);

        comment.IsAdmin = User.Identity!.IsAuthenticated;
        comment.Content = comment.Content.Trim();
        comment.Author = comment.Author.Trim();
        comment.Email = comment.Email.Trim();

        // the website form key should have been removed by javascript unless the comment was posted
        // by a spam robot
        if (!Request.Form.ContainsKey("website"))
        {
            post.Comments.Add(comment);
            await blog.SavePost(post).ConfigureAwait(false);
        }

        return Redirect($"{post.GetEncodedLink()}#{comment.ID}");
    }

    [Route("/blog/category/{category}/{page:int?}")]
    [OutputCache(PolicyName = "default")]
    public async Task<IActionResult> Category(string category, int page = 0)
    {
        // get posts for the selected category.
        var posts = blog.GetPostsByCategory(category);

        int totalPostCount = await posts.CountAsync().ConfigureAwait(true);

        // apply paging filter.
        int postsPerPage = settings.Value.PostsPerPage;
        if (postsPerPage <= 0)
        {
            postsPerPage = 4; // Default value if not set or invalid
        }

        int totalPages = (totalPostCount / postsPerPage) - (totalPostCount % postsPerPage == 0 ? 1 : 0);

        var pagedPosts = posts.Skip(postsPerPage * page).Take(postsPerPage);

        // set the view option
        ViewData[Constants.ViewOption] = settings.Value.ListView;
        ViewData[Constants.PostsPerPage] = postsPerPage;
        ViewData[Constants.TotalPages] = totalPages;
        ViewData[Constants.TotalPostCount] = totalPostCount;
        ViewData[Constants.Title] = $"{manifest.Name} {category}";
        ViewData[Constants.Description] = $"Articles posted in the {category} category";
        ViewData[Constants.Prev] = $"/blog/category/{category}/{page + 1}/";
        ViewData[Constants.Next] = $"/blog/category/{category}/{(page <= 1 ? null : page - 1 + "/")}";

        return View("~/Views/Blog/Index.cshtml", pagedPosts);
    }

    [Route("/blog/comment/{postId}/{commentId}")]
    [Authorize]
    public async Task<IActionResult> DeleteComment(string postId, string commentId)
    {
        var post = await blog.GetPostById(postId).ConfigureAwait(false);
        if (post is null)
        {
            return NotFound();
        }

        var comment = post.Comments.FirstOrDefault(c => c.ID.Equals(commentId, StringComparison.OrdinalIgnoreCase));
        if (comment is null)
        {
            return NotFound();
        }

        _ = post.Comments.Remove(comment);
        await blog.SavePost(post).ConfigureAwait(false);

        return Redirect($"{post.GetEncodedLink()}#comments");
    }

    [Route("/blog/deletepost/{id}")]
    [HttpPost, Authorize, AutoValidateAntiforgeryToken]
    public async Task<IActionResult> DeletePost(string id)
    {
        var existing = await blog.GetPostById(id).ConfigureAwait(false);
        if (existing is null)
        {
            return NotFound();
        }

        await blog.DeletePost(existing).ConfigureAwait(false);
        return Redirect("/");
    }

    [Route("/blog/edit/{id?}")]
    [HttpGet, Authorize]
    public async Task<IActionResult> Edit(string? id)
    {
        var categories = await blog.GetCategories().ToListAsync();
        categories.Sort();
        ViewData[Constants.AllCats] = categories;

        var tags = await blog.GetTags().ToListAsync();
        tags.Sort();
        ViewData[Constants.AllTags] = tags;

        if (string.IsNullOrEmpty(id))
        {
            return View(new Post());
        }

        var post = await blog.GetPostById(id).ConfigureAwait(false);

        return post is null ? NotFound() : View(post);
    }

    [Route("/{page:int?}")]
    [OutputCache(PolicyName = "default")]
    public async Task<IActionResult> Index([FromRoute] int page = 0)
    {
        // get published posts.
        var posts = blog.GetPosts();

        int totalPostCount = await posts.CountAsync().ConfigureAwait(true);

        // apply paging filter.
        int postsPerPage = settings.Value.PostsPerPage;
        if (postsPerPage <= 0)
        {
            postsPerPage = 4; // Default value if not set or invalid
        }

        var pagedPosts = posts.Skip(postsPerPage * page).Take(postsPerPage);

        int totalPages = (totalPostCount / postsPerPage) - (totalPostCount % postsPerPage == 0 ? 1 : 0);

        // set the view option
        ViewData[Constants.ViewOption] = settings.Value.ListView;
        ViewData[Constants.PostsPerPage] = postsPerPage;
        ViewData[Constants.TotalPages] = totalPages;
        ViewData[Constants.TotalPostCount] = totalPostCount;
        ViewData[Constants.Title] = manifest.Name;
        ViewData[Constants.Description] = manifest.Description;
        ViewData[Constants.Prev] = $"/{page + 1}/";
        ViewData[Constants.Next] = $"/{(page <= 1 ? null : $"{page - 1}/")}";

        return View("~/Views/Blog/Index.cshtml", pagedPosts);
    }

    [Route("/blog/{slug?}")]
    [OutputCache(PolicyName = "default")]
    public async Task<IActionResult> Post(string slug)
    {
        var post = await blog.GetPostBySlug(slug).ConfigureAwait(true);

        return post is null ? NotFound() : View(post);
    }

    /// <summary>
    /// Redirects the old Miniblog URL format to the new blog URL format.
    /// </summary>
    /// <param name="slug">The slug.</param>
    /// <returns>The result.</returns>
    [Route("/post/{slug}")]
    [HttpGet]
    public IActionResult Redirects(string slug) => LocalRedirectPermanent($"/blog/{slug}");

    [Route("/blog/tag/{tag}/{page:int?}")]
    [OutputCache(PolicyName = "default")]
    public async Task<IActionResult> Tag(string tag, int page = 0)
    {
        // get posts for the selected tag.
        var posts = blog.GetPostsByTag(tag);

        int totalPostCount = await posts.CountAsync().ConfigureAwait(true);

        // apply paging filter.
        int postsPerPage = settings.Value.PostsPerPage;
        if (postsPerPage <= 0)
        {
            postsPerPage = 4; // Default value if not set or invalid
        }

        int totalPages = (totalPostCount / postsPerPage) - (totalPostCount % postsPerPage == 0 ? 1 : 0);

        var pagedPosts = posts.Skip(postsPerPage * page).Take(postsPerPage);

        // set the view option
        ViewData[Constants.ViewOption] = settings.Value.ListView;
        ViewData[Constants.PostsPerPage] = postsPerPage;
        ViewData[Constants.TotalPages] = totalPages;
        ViewData[Constants.TotalPostCount] = totalPostCount;
        ViewData[Constants.Title] = $"{manifest.Name} {tag}";
        ViewData[Constants.Description] = $"Articles posted in the {tag} tag";
        ViewData[Constants.Prev] = $"/blog/tag/{tag}/{page + 1}/";
        ViewData[Constants.Next] = $"/blog/tag/{tag}/{(page <= 1 ? null : $"{page - 1}/")}";

        return View("~/Views/Blog/Index.cshtml", pagedPosts);
    }

    [Route("/blog/{slug?}")]
    [HttpPost, Authorize, AutoValidateAntiforgeryToken]
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Part of public API that already shipped.")]
    public async Task<IActionResult> UpdatePost(string? slug, Post post)
    {
        if (!ModelState.IsValid)
        {
            return View(nameof(Edit), post);
        }

        ArgumentNullException.ThrowIfNull(post);

        var existing = await blog.GetPostById(post.ID).ConfigureAwait(false) ?? post;
        var existingPostWithSameSlug = await blog.GetPostBySlug(existing.Slug).ConfigureAwait(true);
        if (existingPostWithSameSlug is not null && existingPostWithSameSlug.ID != post.ID)
        {
            existing.Slug = Models.Post.CreateSlug(post.Title + DateTime.UtcNow.ToString("yyyyMMddHHmm"), 50);
        }

        string categories = Request.Form[Constants.Categories]!;
        string tags = Request.Form[Constants.Tags]!;

        existing.Categories.Clear();
        foreach (string? category in categories
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(c => c.Trim().ToLowerInvariant()))
        {
            existing.Categories.Add(category);
        }

        existing.Tags.Clear();
        foreach (string? tag in tags
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

        await SaveFilesToDisk(existing).ConfigureAwait(false);

        await blog.SavePost(existing).ConfigureAwait(false);

        return Redirect(post.GetEncodedLink());
    }

    [GeneratedRegex("data:[^/]+/(?<ext>[a-z]+);base64,(?<base64>.+)", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex Base64Regex();

    [GeneratedRegex("<img[^>]+ />", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex ImgRegex();

    private async Task SaveFilesToDisk(Post post)
    {
        var imgRegex = ImgRegex();
        var base64Regex = Base64Regex();
        string[] allowedExtensions = [
            ".jpg",
            ".jpeg",
            ".gif",
            ".png",
            ".webp"
            ];

        foreach (Match? match in imgRegex.Matches(post.Content))
        {
            if (match is null)
            {
                continue;
            }

            XmlDocument doc = new();
            doc.LoadXml($"<root>{match.Value}</root>");

            var img = doc.FirstChild!.FirstChild;
            var srcNode = img!.Attributes!["src"];
            var fileNameNode = img.Attributes["data-filename"];

            // The HTML editor creates base64 DataURIs which we'll have to convert to image files on disk
            if (srcNode is null || fileNameNode is null)
            {
                continue;
            }

            string extension = Path.GetExtension(fileNameNode.Value);

            // Only accept image files
            if (!allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var base64Match = base64Regex.Match(srcNode.Value);
            if (base64Match.Success)
            {
                byte[] bytes = Convert.FromBase64String(base64Match.Groups["base64"].Value);
                srcNode.Value = await blog.SaveFile(bytes, fileNameNode.Value).ConfigureAwait(false);

                _ = img.Attributes.Remove(fileNameNode);
                post.Content = post.Content.Replace(match.Value, img.OuterXml, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
