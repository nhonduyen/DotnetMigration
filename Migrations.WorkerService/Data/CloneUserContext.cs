using Microsoft.EntityFrameworkCore;
using Migrations.WorkerService.Models;

namespace Migrations.WorkerService.Data
{
    public class CloneUserContext : DbContext
    {
        public CloneUserContext(DbContextOptions<CloneUserContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserProfile>()
                .Property(e => e.RowVersion)
                .IsConcurrencyToken(false);
        }

        public DbSet<UserProfile> UserProfile { get; set; }
    }
}
