namespace Miniblog.Core.Database
{
    using Microsoft.EntityFrameworkCore;

    using Miniblog.Core.Database.Models;

    public class BlogContext : DbContext
    {
        public BlogContext(DbContextOptions<BlogContext> options)
            : base(options)
        {
        }
    }
}
