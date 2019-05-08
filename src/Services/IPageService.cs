using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Miniblog.Core.Models;

namespace Miniblog.Core.Services
{
    public interface IPageService
    {
        Task<Page> GetIndexPage(string index);

        Task<IEnumerable<Page>> GetPages(int count, int skip = 0);

        Task<Page> GetPageBySlug(string slug);

        Task<Page> GetPageById(string id);

        Task SavePage(Page page);

        Task DeletePage(Page page);

        Task<string> SaveFile(byte[] bytes, string fileName, string suffix = null);
    }

    public abstract class InMemoryPageServiceBase : IPageService
    {
        public InMemoryPageServiceBase(IHttpContextAccessor contextAccessor)
        {
            ContextAccessor = contextAccessor;
        }

        protected List<Page> Cache { get; set; }
        protected IHttpContextAccessor ContextAccessor { get; }

        public virtual Task<Page> GetIndexPage(string index)
        {
            var page = Cache.FirstOrDefault(p => p.Slug.Equals(index, StringComparison.OrdinalIgnoreCase));
            bool isAdmin = IsAdmin();

            if (page != null && page.PubDate <= DateTime.UtcNow && (page.IsPublished || isAdmin))
            {
                return Task.FromResult(page);
            }

            return Task.FromResult<Page>(null);
        }

        public virtual Task<IEnumerable<Page>> GetPages(int count, int skip = 0)
        {
            bool isAdmin = IsAdmin();

            var pages = Cache
                .Where(p => p.PubDate <= DateTime.UtcNow && (p.IsPublished || isAdmin))
                .Skip(skip)
                .Take(count);

            return Task.FromResult(pages);
        }

        public virtual Task<Page> GetPageBySlug(string slug)
        {
            var page = Cache.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
            bool isAdmin = IsAdmin();

            if (page != null && page.PubDate <= DateTime.UtcNow && (page.IsPublished || isAdmin))
            {
                return Task.FromResult(page);
            }

            return Task.FromResult<Page>(null);
        }

        public virtual Task<Page> GetPageById(string id)
        {
            var page = Cache.FirstOrDefault(p => p.ID.Equals(id, StringComparison.OrdinalIgnoreCase));
            bool isAdmin = IsAdmin();

            if (page != null && page.PubDate <= DateTime.UtcNow && (page.IsPublished || isAdmin))
            {
                return Task.FromResult(page);
            }

            return Task.FromResult<Page>(null);
        }

        public abstract Task SavePage(Page page);

        public abstract Task DeletePage(Page page);

        public abstract Task<string> SaveFile(byte[] bytes, string fileName, string suffix = null);

        protected void SortCache()
        {
            Cache.Sort((p1, p2) => p2.PubDate.CompareTo(p1.PubDate));
        }

        protected bool IsAdmin()
        {
            return ContextAccessor.HttpContext?.User?.Identity.IsAuthenticated == true;
        }
    }
}
