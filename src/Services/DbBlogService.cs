namespace Miniblog.Core.Services
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;

    using Miniblog.Core.Database;
    using Miniblog.Core.Database.Models;
    using Miniblog.Core.Models;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class DbBlogService : IBlogService
    {
        private readonly IHttpContextAccessor contextAccessor;
        private readonly BlogContext blogContext;

        public DbBlogService(
            IHttpContextAccessor contextAccessor,
            BlogContext blogContext)
        {
            this.contextAccessor = contextAccessor;
            this.blogContext = blogContext;
        }

        public async Task DeletePost(Post post)
        {
            if (post is null)
            {
                throw new ArgumentNullException(nameof(post));
            }

            if (!Guid.TryParse(post.ID, out var postId))
            {
                throw new ArgumentException("Wrong format of post id");
            }

            var postEntity = await this.blogContext.Posts.FindAsync(postId);
            if (postEntity != null)
            {
                this.blogContext.Remove(postEntity);
            }
        }

        public IAsyncEnumerable<string> GetCategories()
        {
            var isAdmin = this.IsAdmin();

            return this.blogContext
                .Categories
                .Where(p => p.Post.IsPublished || isAdmin)
                .Select(cat => cat.Name.ToLowerInvariant())
                .Distinct()
                .ToAsyncEnumerable();
        }

        public IAsyncEnumerable<string> GetTags()
        {
            var isAdmin = this.IsAdmin();

            return this.blogContext
                .Tags
                .Where(p => p.Post.IsPublished || isAdmin)
                .Select(tag => tag.Name.ToLowerInvariant())
                .Distinct()
                .ToAsyncEnumerable();
        }

        public Task<Post?> GetPostById(string id)
        {
            var isAdmin = this.IsAdmin();

            _ = Guid.TryParse(id, out var postId);
            var post = this.blogContext
                .Posts
                .Include(nameof(PostDb.Categories))
                .Include(nameof(PostDb.Tags))
                .FirstOrDefault(p =>
                p.ID == postId
                && ((p.PubDate < DateTime.UtcNow && p.IsPublished)
                    || isAdmin));

            return Task.FromResult(MapEntityToPost(post));
        }

        public Task<Post?> GetPostBySlug(string slug)
        {
            var isAdmin = this.IsAdmin();
            var decodedSlug = System.Net.WebUtility.UrlDecode(slug).ToLower();
            var post = this.blogContext
                .Posts
                .Include(nameof(PostDb.Categories))
                .Include(nameof(PostDb.Tags))
                .FirstOrDefault(p =>
                p.Slug.ToLower().Equals(decodedSlug)
                && ((p.PubDate < DateTime.UtcNow && p.IsPublished)
                    || isAdmin));

            return Task.FromResult(MapEntityToPost(post));
        }

        public IAsyncEnumerable<Post> GetPosts()
        {
            var isAdmin = this.IsAdmin();

            return this.blogContext
                .Posts
                .Include(nameof(PostDb.Categories))
                .Include(nameof(PostDb.Tags))
                .Where(p => (p.PubDate < DateTime.UtcNow && p.IsPublished)
                            || isAdmin)
                .Select(p => MapEntityToPost(p))
                .ToAsyncEnumerable()
                .OrderByDescending(p => p.PubDate);
        }

        public IAsyncEnumerable<Post> GetPosts(int count, int skip = 0)
        {
            var isAdmin = this.IsAdmin();

            return this.blogContext
                .Posts
                .Include(nameof(PostDb.Categories))
                .Include(nameof(PostDb.Tags))
                .Where(p => (p.PubDate < DateTime.UtcNow && p.IsPublished)
                            || isAdmin)
                .Skip(skip)
                .Take(count)
                .Select(p => MapEntityToPost(p))
                .ToAsyncEnumerable();
        }

        public IAsyncEnumerable<Post> GetPostsByCategory(string category) => throw new NotImplementedException();

        public IAsyncEnumerable<Post> GetPostsByTag(string tag) => throw new NotImplementedException();

        public Task<string> SaveFile(byte[] bytes, string fileName, string? suffix = null) => throw new NotImplementedException();

        public async Task SavePost(Post post)
        {
            if (post is null)
            {
                throw new ArgumentNullException(nameof(post));
            }

            _ = Guid.TryParse(post.ID, out var postId);


            var entity = this.blogContext
                .Posts
                .Include(nameof(PostDb.Categories))
                .Include(nameof(PostDb.Tags))
                .FirstOrDefault(p=>p.ID == postId) ?? new PostDb();

            post.LastModified = DateTime.UtcNow;

            BindPostToEntity(post, entity);

            if (entity.ID == Guid.Empty)
            {
                _ = await this.blogContext.Posts.AddAsync(entity);
            }
            else
            {
                this.blogContext.Posts.Update(entity);
            }
            await this.blogContext.SaveChangesAsync();
        }

        protected bool IsAdmin() => this.contextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

        private void BindPostToEntity(Post post, PostDb entity)
        {
            entity.Content = post.Content;
            entity.Excerpt = post.Excerpt;
            entity.IsPublished = post.IsPublished;
            entity.LastModified = post.LastModified;
            entity.PubDate = post.PubDate;
            entity.Slug = post.Slug;
            entity.Title = post.Title;

            entity.Tags = entity.Tags ?? new List<TagDb>();
            entity.Categories = entity.Categories ?? new List<CategoryDb>();

            entity.Tags = entity.Tags.IntersectBy(post.Tags.ToList(), x => x.Name).ToList();

            foreach (var cat in post.Categories)
            {
                if(entity.Categories.Any(c=>c.Name.ToLower() == cat.ToLower()))
                {
                    entity.Categories.Add(new CategoryDb
                    {
                        Name = cat
                    });
                }
            }
        }

        private static Post MapEntityToPost(PostDb post)
        {
            if (post is null)
            {
                return null;
            }

            var postDto = new Post
            {
                ID = post.ID.ToString(),
                Content = post.Content,
                Excerpt = post.Excerpt,
                IsPublished = post.IsPublished,
                LastModified = post.LastModified,
                PubDate = post.PubDate,
                Slug = post.Slug,
                Title = post.Title
            };

            foreach (var tag in post.Tags)
            {
                postDto.Tags.Add(tag.Name);
            }

            foreach (var cat in post.Categories)
            {
                postDto.Categories.Add(cat.Name);
            }

            return postDto;
        }
    }
}
