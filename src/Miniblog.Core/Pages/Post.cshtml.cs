using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Miniblog.Core;

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
            return Page();
        }
    }
}
