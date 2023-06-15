using Migrations.API.Models;
using System.ComponentModel.DataAnnotations;

namespace Migrations.WorkerService.Models
{
    public class UserProfile : BaseEntity
    {
        [MaxLength(100)]
        public string Name { get; set; }
        [MaxLength(50)]
        public string Email { get; set; }
        [MaxLength(50)]
        public string Phone { get; set; }
    }
}
