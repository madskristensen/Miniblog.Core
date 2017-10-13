using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace Miniblog.Core
{
    public class BlogController : Controller
    {
        private IBlogService _blog;
        private IOptionsSnapshot<BlogSettings> _settings;

        public BlogController(IBlogService blog, IOptionsSnapshot<BlogSettings> settings)
        {
            _blog = blog;
            _settings = settings;
        }

        [Route("/{page:int?}")]
        public async Task<IActionResult> Index([FromRoute]int page = 0)
        {
            var posts = await _blog.GetPosts(_settings.Value.PostsPerPage, _settings.Value.PostsPerPage * page);
            ViewData["Title"] = _settings.Value.Name;
            ViewData["Description"] = _settings.Value.Description;
            ViewData["prev"] = $"/{page + 1}/";
            ViewData["next"] = $"/{(page <= 1 ? null : page - 1 + "/")}";
            return View("views/blog/index.cshtml", posts);
        }

        [Route("/blog/category/{category}/{page:int?}")]
        public async Task<IActionResult> Category(string category, int page = 0)
        {
            var posts = (await _blog.GetPostsByCategory(category)).Skip(_settings.Value.PostsPerPage * page).Take(_settings.Value.PostsPerPage);
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
        public async Task<IActionResult> Post(string slug, [FromQuery] bool edit)
        {
            var post = await _blog.GetPostBySlug(slug);

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
        public async Task<IActionResult> UpdatePost(Post post)
        {
            if (!ModelState.IsValid)
            {
                return View("Edit", post);
            }

            var existing = await _blog.GetPostById(post.ID) ?? post;
            string categories = Request.Form["categories"];

            existing.Categories = categories.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim().ToLowerInvariant()).ToList();
            existing.Title = post.Title.Trim();
            existing.Slug = post.Slug.Trim();
            existing.Slug = !string.IsNullOrWhiteSpace(post.Slug) ? post.Slug.Trim() : Core.Post.CreateSlug(post.Title);
            existing.IsPublished = post.IsPublished;
            existing.Content = post.Content.Trim();
            existing.Excerpt = post.Excerpt.Trim();

            await _blog.SavePost(existing);

            await SaveFilesToDisk(existing);

            return Redirect(post.GetLink());
        }

        private async Task SaveFilesToDisk(Post post)
        {
            var imgRegex = new Regex("<img[^>].+ />", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var base64Regex = new Regex("data:[^/]+/(?<ext>[a-z]+);base64,(?<base64>.+)", RegexOptions.IgnoreCase);

            foreach (Match match in imgRegex.Matches(post.Content))
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml("<root>" + match.Value + "</root>");

                var img = doc.FirstChild.FirstChild;
                var srcNode = img.Attributes["src"];
                var fileNameNode = img.Attributes["data-filename"];

                // The HTML editor creates base64 DataURIs which we'll have to convert to image files on disk
                if (srcNode != null && fileNameNode != null)
                {
                    var base64Match = base64Regex.Match(srcNode.Value);
                    if (base64Match.Success)
                    {
                        byte[] bytes = Convert.FromBase64String(base64Match.Groups["base64"].Value);
                        srcNode.Value = await _blog.SaveFile(bytes, fileNameNode.Value).ConfigureAwait(false);

                        img.Attributes.Remove(fileNameNode);
                        post.Content = post.Content.Replace(match.Value, img.OuterXml);
                    }
                }
            }
        }

        private byte[] ConvertToBytes(string base64)
        {
            int index = base64.IndexOf("base64,", StringComparison.Ordinal) + 7;
            return Convert.FromBase64String(base64.Substring(index));
        }

        [Route("/blog/deletepost/{slug}")]
        [HttpPost, Authorize, AutoValidateAntiforgeryToken]
        public async Task<IActionResult> DeletePost(string id)
        {
            var existing = await _blog.GetPostById(id);

            if (existing != null)
            {
                await _blog.DeletePost(existing);
                return Redirect("/");
            }

            return NotFound();
        }

        [Route("/blog/comment/{postId}")]
        [HttpPost, AutoValidateAntiforgeryToken]
        public async  Task<IActionResult> AddComment(string postId, Comment comment)
        {
            if (!ModelState.IsValid)
            {
                return View("Post");
            }

            var post = await _blog.GetPostById(postId);

            if (post == null)
            {
                return NotFound();
            }

            comment.IsAdmin = User.Identity.IsAuthenticated;
            comment.Content = comment.Content.Trim();
            comment.Author = comment.Author.Trim();
            comment.Email = comment.Email.Trim();

            post.Comments.Add(comment);
            await _blog.SavePost(post);

            return Redirect(post.GetLink() + "#" + comment.ID);
        }

        [Route("/blog/comment/{postId}/{commentId}")]
        [Authorize]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> DeleteComment(string postId, string commentId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            var post = await _blog.GetPostById(postId);

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
            await _blog.SavePost(post);

            return Redirect(post.GetLink() + "#comments");
        }
    }
}
