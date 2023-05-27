using System.ComponentModel.DataAnnotations;

namespace Migrations.WorkerService.Models
{
    public class UserProfile
    {
        public Guid Id { get; set; }

        [MaxLength(100)]
        public string Name { get; set; }
        [MaxLength(50)]
        public string Email { get; set; }
        [MaxLength(50)]
        public string Phone { get; set; }
        [MaxLength(18)]
        public byte[] RowVersion { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedTime { get; set; }
    }
}
