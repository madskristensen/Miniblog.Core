using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Miniblog.Core
{
    public class BlogController : Controller
    {
        private IBlogStorage _storage;
        private IOptionsSnapshot<BlogSettings> _settings;
        private RenderingService _rs;

        public BlogController(IBlogStorage storage, IOptionsSnapshot<BlogSettings> settings, RenderingService rs)
        {
            _storage = storage;
            _settings = settings;
            _rs = rs;
        }

        [Route("/")]
        public IActionResult Index()
        {
            var posts = _storage.GetPosts(5);
            return View(posts);
        }

        [Route("/category/{category}")]
        public IActionResult Category(string category)
        {
            var posts = _storage.GetPostsByCategory(category);
            return View("Index", posts);
        }

        [Route("/post/{slug?}")]
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

        [Route("/post/{slug?}")]
        [HttpPost, Authorize, AutoValidateAntiforgeryToken]
        public async Task<IActionResult> UpdatePost(Post post)
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
            existing.IsPublished = post.IsPublished;
            existing.Content = post.Content.Trim();
            existing.Excerpt = post.Excerpt.Trim();

            await _storage.SavePost(existing);

            return Redirect(post.GetLink());
        }

        [Route("/deletepost/{slug}")]
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

        [Route("/comment/{postId}")]
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

        [Route("/comment/{postId}/{commentId}")]
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
