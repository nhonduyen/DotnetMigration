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
        }

        public DbSet<UserProfile> UserProfile { get; set; }
    }
}
