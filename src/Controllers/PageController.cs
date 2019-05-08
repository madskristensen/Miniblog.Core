using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Miniblog.Core.Models;
using Miniblog.Core.Services;
using WebEssentials.AspNetCore.Pwa;

namespace Miniblog.Core.Controllers
{
    public class PageController : Controller
    {
        private readonly IPageService _page;
        private readonly IOptionsSnapshot<PageSettings> _settings;
        private readonly WebManifest _manifest;

        public PageController(IPageService page, IOptionsSnapshot<PageSettings> settings, WebManifest manifest)
        {
            _page = page;
            _settings = settings;
            _manifest = manifest;
        }

        [Route("/")]
        [OutputCache(Profile = "default")]
        public async Task<IActionResult> Index()
        {
            var page = await _page.GetIndexPage(_settings.Value.Index);
            ViewData["Title"] = _manifest.Name;
            ViewData["Description"] = _manifest.Description;
            return View("~/Views/Page/Index.cshtml", page);
        }

        [Route("/{slug?}")]
        [OutputCache(Profile = "default")]
        public async Task<IActionResult> Page(string slug)
        {
            var page = await _page.GetPageBySlug(slug);

            if (page != null)
            {
                return View("~/Views/Page/Page.cshtml", page);
            }

            return NotFound();
        }

        [Route("/page/edit/{id?}")]
        [HttpGet, Authorize]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return View(new Page());
            }

            var page = await _page.GetPageById(id);

            if (page != null)
            {
                return View(page);
            }

            return NotFound();
        }

        [Route("/page/{slug?}")]
        [HttpPost, Authorize, AutoValidateAntiforgeryToken]
        public async Task<IActionResult> UpdatePage(Page page)
        {
            if (!ModelState.IsValid)
            {
                return View("Edit", page);
            }

            var existing = await _page.GetPageById(page.ID) ?? page;

            existing.Title = page.Title.Trim();
            existing.Slug = !string.IsNullOrWhiteSpace(page.Slug) ? page.Slug.Trim() : Models.Page.CreateSlug(page.Title);
            existing.IsPublished = page.IsPublished;
            existing.Content = page.Content.Trim();
            existing.Excerpt = page.Excerpt.Trim();

            await SaveFilesToDisk(existing);

            await _page.SavePage(existing);

            return Redirect(page.GetEncodedLink());
        }

        private async Task SaveFilesToDisk(Page page)
        {
            var imgRegex = new Regex("<img[^>].+ />", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var base64Regex = new Regex("data:[^/]+/(?<ext>[a-z]+);base64,(?<base64>.+)", RegexOptions.IgnoreCase);
            string[] allowedExtensions = new[] {
              ".jpg",
              ".jpeg",
              ".gif",
              ".png",
              ".webp"
            };

            foreach (Match match in imgRegex.Matches(page.Content))
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml("<root>" + match.Value + "</root>");

                var img = doc.FirstChild.FirstChild;
                var srcNode = img.Attributes["src"];
                var fileNameNode = img.Attributes["data-filename"];

                // The HTML editor creates base64 DataURIs which we'll have to convert to image files on disk
                if (srcNode != null && fileNameNode != null)
                {
                    string extension = System.IO.Path.GetExtension(fileNameNode.Value);

                    // Only accept image files
                    if (!allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var base64Match = base64Regex.Match(srcNode.Value);
                    if (base64Match.Success)
                    {
                        byte[] bytes = Convert.FromBase64String(base64Match.Groups["base64"].Value);
                        srcNode.Value = await _page.SaveFile(bytes, fileNameNode.Value).ConfigureAwait(false);

                        img.Attributes.Remove(fileNameNode);
                        page.Content = page.Content.Replace(match.Value, img.OuterXml);
                    }
                }
            }
        }

        [Route("/blog/deletepage/{id}")]
        [HttpPost, Authorize, AutoValidateAntiforgeryToken]
        public async Task<IActionResult> DeletePage(string id)
        {
            var existing = await _page.GetPageById(id);

            if (existing != null)
            {
                await _page.DeletePage(existing);
                return Redirect("/");
            }

            return NotFound();
        }
    }
}
