using Microsoft.EntityFrameworkCore;
using Migrations.API.Models;

namespace Migrations.API.Data
{
    public class UserContext : DbContext
    {
        public UserContext(DbContextOptions<UserContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserProfile>().Property(e => e.RowVersion).IsRowVersion();
        }

        public DbSet<UserProfile> UserProfile { get; set; }
    }
}
