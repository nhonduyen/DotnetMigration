using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Migrations.API.Models
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
        [Timestamp]
        public byte[] RowVersion { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedTime { get; set; }
    }
}
