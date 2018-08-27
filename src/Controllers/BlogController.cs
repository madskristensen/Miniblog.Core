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
    public class BlogController : Controller
    {
        private readonly IBlogService _blog;
        private readonly IOptionsSnapshot<BlogSettings> _settings;
        private readonly WebManifest _manifest;

        public BlogController(IBlogService blog, IOptionsSnapshot<BlogSettings> settings, WebManifest manifest)
        {
            _blog = blog;
            _settings = settings;
            _manifest = manifest;
        }

        [Route("/{page:int?}")]
        [OutputCache(Profile = "default")]
        public async Task<IActionResult> Index([FromRoute]int page = 0)
        {
            var posts = await _blog.GetPosts(_settings.Value.PostsPerPage, _settings.Value.PostsPerPage * page);
            ViewData["Title"] = _manifest.Name;
            ViewData["Description"] = _manifest.Description;
            ViewData["prev"] = $"/{page + 1}/";
            ViewData["next"] = $"/{(page <= 1 ? null : page - 1 + "/")}";
            return View("~/Views/Blog/Index.cshtml", posts);
        }

        [Route("/blog/category/{category}/{page:int?}")]
        [OutputCache(Profile = "default")]
        public async Task<IActionResult> Category(string category, int page = 0)
        {
            var posts = (await _blog.GetPostsByCategory(category)).Skip(_settings.Value.PostsPerPage * page).Take(_settings.Value.PostsPerPage);
            ViewData["Title"] = _manifest.Name + " " + category;
            ViewData["Description"] = $"Articles posted in the {category} category";
            ViewData["prev"] = $"/blog/category/{category}/{page + 1}/";
            ViewData["next"] = $"/blog/category/{category}/{(page <= 1 ? null : page - 1 + "/")}";
            return View("~/Views/Blog/Index.cshtml", posts);
        }

        [Route("/blog/archive/{searching?}/{year?}/{month?}")]
        [OutputCache(Profile = "default")]
        public async Task<IActionResult> Archive(string searching, int year, int month)
        {
            var archiveItems = new System.Collections.Generic.List<Category>();

            if (!string.IsNullOrWhiteSpace(searching))
            {
                var date = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                var firstPost = await _blog.GetFirstPost();
                var lastPost = (await _blog.GetPosts(1)).FirstOrDefault();

                if (firstPost == null || lastPost == null)
                {
                    throw new ArgumentException("no post found!");
                }

                date = year == 0 && month == 0
                    ? lastPost.PubDate.Date
                    : new DateTime(year, month, 1);

                date = (date.Date < firstPost.PubDate.Date)
                    ? firstPost.PubDate.Date
                    : date;

                date = (date.Date > DateTime.UtcNow.Date)
                    ? lastPost.PubDate.Date
                    : date;

                var firstDay = new DateTime(date.Year, date.Month, 1);
                var lastDay = firstDay.AddMonths(1).AddDays(-1);
                FillFullWeeks(ref firstDay, ref lastDay);
                var allPostInSpan = await _blog.GetPostsByTimeSpan(firstDay, lastDay);
                while (!allPostInSpan.Any())
                {
                    if (searching.StartsWith("cal__p"))
                    {
                        var allPostToFirst = await _blog.GetPostsByTimeSpan(DateTime.MinValue, firstDay);
                        if (!allPostToFirst.Any())
                        {
                            if (firstPost != null)
                            {
                                date = firstPost.PubDate.Date;
                                firstDay = new DateTime(date.Year, date.Month, 1);
                                lastDay = firstDay.AddMonths(1).AddDays(-1);
                                FillFullWeeks(ref firstDay, ref lastDay);
                                allPostInSpan = await _blog.GetPostsByTimeSpan(firstDay, lastDay);
                            }
                            break;
                        }
                    }
                    date = date.AddMonths(searching.StartsWith("cal__n") ? 1 : -1);
                    if (date.Date == DateTime.MinValue.Date) break;
                    firstDay = new DateTime(date.Year, date.Month, 1);
                    lastDay = firstDay.AddMonths(1).AddDays(-1);
                    FillFullWeeks(ref firstDay, ref lastDay);
                    allPostInSpan = await _blog.GetPostsByTimeSpan(firstDay, lastDay);
                }
                var days = (lastDay - firstDay).TotalDays;

                archiveItems.Add(new Category());
                for (var day = firstDay; day <= lastDay; day = day.AddDays(1))
                {
                    var posts = from p in allPostInSpan
                                where p.PubDate.Date.Equals(day.Date)
                                select p;

                    if (posts != null && posts.Count() > 0)
                    {
                        if (posts.Count() == 1)
                        {
                            archiveItems[0].Posts.Add(posts.FirstOrDefault());
                        }
                        else
                        {
                            archiveItems[0].Posts.Add(new Post
                            {
                                PubDate = day,
                                Title = posts.FirstOrDefault().Title + "..",
                                Slug = day.ToString("yyyyMMdd") + "default-date",
                                Content = ".."
                            });
                        }
                    }
                    else
                    {
                        archiveItems[0].Posts.Add(new Post { PubDate = day });
                    }
                }

                ViewBag.PreviousMonth = firstPost.PubDate.Date < date.AddMonths(-1).Date ? date.AddMonths(-1).Date : firstPost.PubDate.Date;

                ViewBag.NextMonth = lastPost.PubDate.Date > date.AddMonths(1).Date ? date.AddMonths(1).Date : lastPost.PubDate.Date;

                ViewBag.CurrentMonth = date.Date;

                ViewBag.PreviousYear = firstPost.PubDate.Date < date.AddYears(-1).Date ? date.AddYears(-1).Date : firstPost.PubDate.Date;

                ViewBag.NextYear = lastPost.PubDate.Date > date.AddYears(1).Date ? date.AddYears(1).Date : lastPost.PubDate.Date;
            }
            else
            {
                var categoriesCounts = await _blog.GetCategoriesCount();

                foreach (var count in categoriesCounts)
                {
                    var category = new Category()
                    {
                        Count = count.Count,
                        Name = count.Name
                    };

                    var posts = await _blog.GetPostsByCategory(count.Name);

                    var postList = new System.Collections.Generic.List<Post>();
                    foreach (var post in posts)
                    {
                        postList.Add(post);
                    }

                    postList.Sort((p1, p2) => p2.PubDate.CompareTo(p1.PubDate));
                    category.Posts = postList;

                    archiveItems.Add(category);
                }
            }

            ViewData["Title"] = _manifest.Name + " Archive";
            ViewData["LastSearch"] = !string.IsNullOrWhiteSpace(searching) && !searching.StartsWith("cal__") ? searching : "";

            return View("~/Views/Blog/Archive.cshtml", archiveItems);
        }

        // This is for redirecting potential existing URLs from the old Miniblog URL format
        [Route("/post/{slug}")]
        [HttpGet]
        public IActionResult Redirects(string slug)
        {
            return LocalRedirectPermanent($"/blog/{slug}");
        }

        [Route("/blog/{slug?}")]
        [OutputCache(Profile = "default")]
        public async Task<IActionResult> Post(string slug)
        {
            // some older imported posts based on guid or ticks have empty slug, so we link them by ID
            var post = (Guid.TryParse(slug, out Guid g) || long.TryParse(slug, out long l))
                ? await _blog.GetPostById(slug)
                : await _blog.GetPostBySlug(slug);

            if (post != null)
            {
                return View(post);
            }

            // we are here processing the archived calendar links
            else if (slug != null && slug.Length > 8 && int.TryParse(new string(slug.Take(8).ToArray()), out int i))
            {
                var dateSlug = string.Join("/", i.ToString().Substring(0, 4), i.ToString().Substring(4, 2), i.ToString().Substring(6, 2));

                var defaultSlug = slug.Substring(8);

                if (DateTime.TryParse(dateSlug, out DateTime d))
                {
                    if (defaultSlug.Equals("default-year"))
                    {
                        var posts = await _blog.GetPostsByTimeSpan(new DateTime(d.Year, 1, 1), new DateTime(d.Year + 1, 1, 1).AddSeconds(-1));

                        if (posts.Any())
                        {
                            ViewData["Title"] = _manifest.Name + " " + $"{d:yyyy}";
                            ViewData["Description"] = $"Articles posted over the year {d:yyyy}";
                            return View("~/Views/Blog/Index.cshtml", posts);
                        }
                    }
                    else if (defaultSlug.Equals("default-month"))
                    {
                        var posts = await _blog.GetPostsByMonth(d);
                        if (posts.Any())
                        {
                            ViewData["Title"] = _manifest.Name + " " + $"{d:yyyy MMMM}";
                            ViewData["Description"] = $"Articles posted on month {d:yyyy MMMM}";
                            return View("~/Views/Blog/Index.cshtml", posts);
                        }
                    }
                    else if (defaultSlug.Equals("default-date"))
                    {
                        var posts = await _blog.GetPostsByDate(d);

                        if (posts.Any())
                        {
                            ViewData["Title"] = _manifest.Name + " " + $"{d:yyyy MM dd}";
                            ViewData["Description"] = $"Articles posted on day {d:yyyy MM dd}";
                            return View("~/Views/Blog/Index.cshtml", posts);
                        }
                    }
                }
            }

            return NotFound();
        }

        [Route("/blog/edit/{id?}")]
        [HttpGet, Authorize]
        public async Task<IActionResult> Edit(string id)
        {
            ViewData["AllCats"] = (await _blog.GetCategories()).ToList();

            if (string.IsNullOrEmpty(id))
            {
                return View(new Post());
            }

            var post = await _blog.GetPostById(id);

            if (post != null)
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
            existing.Slug = !string.IsNullOrWhiteSpace(post.Slug) ? post.Slug.Trim() : Models.Post.CreateSlug(post.Title);
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

        [Route("/blog/deletepost/{id}")]
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
        [HttpPost]
        public async Task<IActionResult> AddComment(string postId, Comment comment)
        {
            var post = await _blog.GetPostById(postId);

            if (!ModelState.IsValid)
            {
                return View("Post", post);
            }

            if (post == null || !post.AreCommentsOpen(_settings.Value.CommentsCloseAfterDays))
            {
                return NotFound();
            }

            comment.IsAdmin = User.Identity.IsAuthenticated;
            comment.Content = comment.Content.Trim();
            comment.Author = comment.Author.Trim();
            comment.Email = comment.Email.Trim();

            // the website form key should have been removed by javascript
            // unless the comment was posted by a spam robot
            if (!Request.Form.ContainsKey("website"))
            {
                post.Comments.Add(comment);
                await _blog.SavePost(post);
            }

            return Redirect(post.GetLink() + "#" + comment.ID);
        }

        [Route("/blog/comment/{postId}/{commentId}")]
        [Authorize]
        public async Task<IActionResult> DeleteComment(string postId, string commentId)
        {
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

        private static void FillFullWeeks(ref DateTime firstDay, ref DateTime lastDay)
        {
            var firstDayOfWeek = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
            var lastDayOfWeek = firstDayOfWeek.Equals(DayOfWeek.Monday) ? DayOfWeek.Sunday : DayOfWeek.Saturday;

            while (!firstDay.DayOfWeek.Equals(firstDayOfWeek))
            {
                firstDay = firstDay.AddDays(-1);
            }
            while (!lastDay.DayOfWeek.Equals(lastDayOfWeek))
            {
                lastDay = lastDay.AddDays(1);
            }
        }
    }
}
