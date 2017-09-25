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

        public Post Post { get; private set; }

        public IActionResult OnGet(string id = null)
        {
            if (string.IsNullOrEmpty(id))
            {
                Post = new Post();
            }
            else
            {
                Post = _storage.GetPostById(id);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Post post)
        {
            var existing = _storage.GetPostById(post.ID) ?? post;

            existing.Title = post.Title.Trim();
            existing.Slug = post.Slug.Trim();
            existing.IsPublished = post.IsPublished;
            existing.Content = post.Content.Trim();

            await _storage.Save(existing);

            return Redirect(post.GetLink());
        }
    }
}
