using Microsoft.EntityFrameworkCore;
using Miniblog.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Miniblog.Core.Data
{
    public class MiniblogDbContext : DbContext
    {
        public MiniblogDbContext(DbContextOptions options) : base(options) { }

        public DbSet<Post> Posts { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Post>()
                .HasMany(p => p.Comments)
                .WithOne(c => c.Post)
                .HasForeignKey(c => c.PostID);
        }
    }
}
