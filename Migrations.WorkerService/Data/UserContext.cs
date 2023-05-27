using Microsoft.EntityFrameworkCore;
using Migrations.WorkerService.Models;

namespace Migrations.WorkerService.Data
{
    public class UserContext : DbContext
    {
        public UserContext(DbContextOptions<UserContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }

        public DbSet<UserProfile> UserProfile { get; set; }
    }
}
