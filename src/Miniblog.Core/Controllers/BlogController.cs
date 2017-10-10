using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Miniblog.Core
{
    public class BlogController : Controller
    {
        private IBlogStorage _storage;
        private IOptionsSnapshot<BlogSettings> _settings;

        public BlogController(IBlogStorage storage, IOptionsSnapshot<BlogSettings> settings)
        {
            _storage = storage;
            _settings = settings;
        }

        [Route("/{page:int?}")]
        public IActionResult Index([FromRoute]int page = 0)
        {
            var posts = _storage.GetPosts(_settings.Value.PostsPerPage, _settings.Value.PostsPerPage * page);
            ViewData["Title"] = _settings.Value.Name;
            ViewData["Description"] = _settings.Value.Description;
            ViewData["prev"] = $"/{page + 1}/";
            ViewData["next"] = $"/{(page <= 1 ? null : page - 1 + "/")}";
            return View("views/blog/index.cshtml", posts);
        }

        [Route("/blog/category/{category}/{page:int?}")]
        public IActionResult Category(string category, int page = 0)
        {
            var posts = _storage.GetPostsByCategory(category).Skip(_settings.Value.PostsPerPage * page).Take(_settings.Value.PostsPerPage);
            ViewData["Title"] = _settings.Value.Name + " " + category;
            ViewData["Description"] = $"Articles posted in the {category} category";
            ViewData["prev"] = $"/blog/category/{category}/{page + 1}/";
            ViewData["next"] = $"/blog/category/{category}/{(page <= 1 ? null : page - 1 + "/")}";
            return View("views/blog/index.cshtml", posts);
        }

        // This is for redirecting potential existing URLs from the old Miniblog URL format
        [Route("/post/{slug}")]
        [HttpGet]
        public IActionResult Redirects(string slug)
        {
            return RedirectToActionPermanent("Post");
        }

        [Route("/blog/{slug?}")]
        [HttpGet]
        public IActionResult Post(string slug, [FromQuery] bool edit)
        {
            var post = _storage.GetPostBySlug(slug);

            if (edit && User.Identity.IsAuthenticated)
            {
                return View("Edit", post ?? new Post());
            }
            else if (post != null)
            {
                return View(post);
            }

            return NotFound();
        }

        [Route("/blog/{slug?}")]
        [HttpPost, Authorize, AutoValidateAntiforgeryToken]
        public async Task<IActionResult> UpdatePost(Post post, List<IFormFile> files)
        {
            if (!ModelState.IsValid)
            {
                return View("Edit", post);
            }

            var existing = _storage.GetPostById(post.ID) ?? post;
            string categories = Request.Form["categories"];

            existing.Categories = categories.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim().ToLowerInvariant()).ToList();
            existing.Title = post.Title.Trim();
            existing.Slug = post.Slug.Trim();
            existing.Slug = !string.IsNullOrWhiteSpace(post.Slug) ? post.Slug.Trim() : Core.Post.CreateSlug(post.Title);
            existing.IsPublished = post.IsPublished;
            existing.Content = post.Content.Trim();
            existing.Excerpt = post.Excerpt.Trim();

            await _storage.SavePost(existing);

            foreach (var formFile in files.Where(f => f.Length > 0))
            {
                using (var ms = new MemoryStream())
                {
                    await formFile.CopyToAsync(ms);
                    var bytes = ms.ToArray();
                    _storage.SaveFile(bytes, formFile.FileName, existing.ID);
                }
            }

            return Redirect(post.GetLink());
        }

        [Route("/blog/deletepost/{slug}")]
        [HttpPost, Authorize, AutoValidateAntiforgeryToken]
        public IActionResult DeletePost(string id)
        {
            var existing = _storage.GetPostById(id);

            if (existing != null)
            {
                _storage.DeletePost(existing);
                return Redirect("/");
            }

            return NotFound();
        }

        [Route("/blog/comment/{postId}")]
        [HttpPost, AutoValidateAntiforgeryToken]
        public IActionResult AddComment(string postId, Comment comment)
        {
            if (!ModelState.IsValid)
            {
                return View("Post");
            }

            var post = _storage.GetPostById(postId);

            if (post == null)
            {
                return NotFound();
            }

            comment.IsAdmin = User.Identity.IsAuthenticated;
            comment.Content = comment.Content.Trim();

            post.Comments.Add(comment);
            _storage.SavePost(post);

            return Redirect(post.GetLink() + "#" + comment.ID);
        }

        [Route("/blog/comment/{postId}/{commentId}")]
        [Authorize]
        [AutoValidateAntiforgeryToken]
        public IActionResult DeleteComment(string postId, string commentId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            var post = _storage.GetPostById(postId);

            if (post == null)
            {
                return NotFound();
            }

            var comment = post.Comments.FirstOrDefault(c => c.ID.Equals(commentId, StringComparison.OrdinalIgnoreCase));

            if (comment == null)
            {
                return NotFound();
            }

            post.Comments.Remove(comment);
            _storage.SavePost(post);

            return Redirect(post.GetLink() + "#comments");
        }
    }
}
