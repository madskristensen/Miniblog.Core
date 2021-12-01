namespace Miniblog.Core.Database
{
    using Microsoft.EntityFrameworkCore;

    using Miniblog.Core.Database.Models;

    public class BlogContext : DbContext
    {
        public DbSet<PostDb> Posts { get; set; }
        public DbSet<CategoryDb> Categories { get; set; }
        public DbSet<TagDb> Tags { get; set; }
        public DbSet<CommentDb> Comments { get; set; }

        public BlogContext(DbContextOptions<BlogContext> options)
            : base(options)
        {
        }
    }
}
