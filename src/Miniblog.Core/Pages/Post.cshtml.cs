using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Miniblog.Core;
using System;
using System.Linq;

namespace Miniblog.Core.Pages
{
    public class PostPageModel : PageModel
    {
        private IBlogStorage _storage;

        public PostPageModel(IBlogStorage storage)
        {
            _storage = storage;
        }

        public Post Post { get; private set; }

        public IActionResult OnGet(string slug)
        {
            Post = _storage.GetPostBySlug(slug);

            if (Post == null)
            {
                return NotFound();
            }

            return Page();
        }

        public IActionResult OnPostComment(string id, [FromForm] Comment comment)
        {
            var post = _storage.GetPostById(id);

            if (post == null)
            {
                return NotFound();
            }

            comment.IsAdmin = User.Identity.IsAuthenticated;
            comment.Content = comment.Content.Trim();

            post.Comments.Add(comment);
            _storage.Save(post);

            return Redirect(post.GetLink() + "#" + comment.ID);
        }

        [Authorize]
        public IActionResult OnGetDeleteComment(string postId, string commentId)
        {
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
            _storage.Save(post);

            return Redirect(post.GetLink() + "#comments");
        }
    }
}
