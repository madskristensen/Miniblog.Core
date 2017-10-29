using Microsoft.EntityFrameworkCore;
using Miniblog.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Miniblog.Core
{
    public class MiniblogDbContext : DbContext
    {
        public MiniblogDbContext(DbContextOptions options) : base(options)
        {
            Database.EnsureCreated();
        }

        public DbSet<Post> Posts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<PostCategory> PostCategories { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Post>()
                .HasMany(p => p.Comments)
                .WithOne(c => c.Post)
                .HasForeignKey(c => c.PostID)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PostCategory>()
                .HasKey(p => new { p.CategoryID, p.PostID });

            builder.Entity<PostCategory>()
                .HasOne(p => p.Post)
                .WithMany(p => p.PostCategories)
                .HasForeignKey(p => p.PostID);

            builder.Entity<PostCategory>()
                .HasOne(p => p.Category)
                .WithMany(c => c.PostCategories)
                .HasForeignKey(p => p.CategoryID);
        }
    }
}
