using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Miniblog.Core;
using System.Collections.Generic;

namespace Miniblog.Core.Pages
{
    public class IndexPageModel : PageModel
    {
        private IBlogStorage _storage;

        public IndexPageModel(IBlogStorage storage)
        {
            _storage = storage;
        }

        public IEnumerable<Post> Posts { get; private set; }

        public IActionResult OnGet()
        {
            Posts = _storage.GetPosts(5);
            return Page();
        }
    }
}
