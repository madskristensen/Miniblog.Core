using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Miniblog.Core;
using System.Threading.Tasks;

namespace Miniblog.Core.Pages
{
    public class EditPageModel : PageModel
    {
        private IBlogStorage _storage;

        public EditPageModel(IBlogStorage storage)
        {
            _storage = storage;
        }

        public Post Post { get; private set; }

        public IActionResult OnGet(string id)
        {
            Post = _storage.GetPostById(id);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Post post)
        {
            var model = _storage.GetPostById(post.ID);
            model.Title = post.Title.Trim();
            model.Slug = post.Slug.Trim();
            model.IsPublished = post.IsPublished;
            model.Content = post.Content.Trim();

            await _storage.Save(model);

            return Redirect(post.GetLink());
        }
    }
}
