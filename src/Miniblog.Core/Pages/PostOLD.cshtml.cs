using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

        [BindProperty]
        public Comment Comment { get; set; }

        public IActionResult OnGet(string slug)
        {
            Post = _storage.GetPostBySlug(slug);

            if (Post == null)
            {
                return NotFound();
            }

            return Page();
        }

        public IActionResult OnPostComment(string id)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var post = _storage.GetPostById(id);

            if (post == null)
            {
                return NotFound();
            }

            Comment.IsAdmin = User.Identity.IsAuthenticated;
            Comment.Content = Comment.Content.Trim();

            post.Comments.Add(Comment);
            _storage.SavePost(post);

            return Redirect(post.GetLink() + "#" + Comment.ID);
        }

        public IActionResult OnGetDeleteComment(string postId, string commentId)
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
