using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Miniblog.Core;
using System.Threading.Tasks;

namespace Miniblog.Core.Pages
{
    [Authorize]
    public class EditPageModel : PageModel
    {
        private IBlogStorage _storage;

        public EditPageModel(IBlogStorage storage)
        {
            _storage = storage;
        }

        [BindProperty]
        public Post Post { get; set; }

        public bool IsNew { get; private set; }

        public IActionResult OnGet(string id = null)
        {
            if (string.IsNullOrEmpty(id))
            {
                Post = new Post();
                IsNew = true;
            }
            else
            {
                Post = _storage.GetPostById(id);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var existing = _storage.GetPostById(Post.ID) ?? Post;

            existing.Title = Post.Title.Trim();
            existing.Slug = Post.Slug.Trim();
            existing.IsPublished = Post.IsPublished;
            existing.Content = Post.Content.Trim();
            existing.Excerpt = Post.Excerpt.Trim();

            await _storage.SavePost(existing);

            return Redirect(Post.GetLink());
        }

        public IActionResult OnPostDelete()
        {
            var existing = _storage.GetPostById(Post.ID);

            if (existing != null)
            {
                _storage.DeletePost(existing);
                return Redirect("/");
            }

            return NotFound();
        }
    }
}
