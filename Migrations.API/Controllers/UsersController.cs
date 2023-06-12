using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Migrations.API.Data;
using Migrations.API.Models;
using Faker;
using System.ComponentModel.DataAnnotations;

namespace Migrations.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UserContext _context;
        private readonly ILogger<UsersController> _logger;

        public UsersController(UserContext context, ILogger<UsersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult> CreateMany(int quantity, CancellationToken cancellationToken)
        {
            var users = new List<UserProfile>(quantity);
            for (int i = 0; i < quantity; i++)
            {
                var now = DateTime.UtcNow;
                var user = new UserProfile
                {
                    Id = Guid.NewGuid(),
                    Name = Faker.Name.FullName(),
                    Phone = Faker.Phone.Number(),
                    Email = Faker.Internet.Email(),
                    CreatedAt = now,
                    LastUpdatedTime = now
                };
                users.Add(user);
            }

            _context.UserProfile.AddRange(users);
            var result = await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation($"{result} items have been created");
            return Ok(users);
        }

        [HttpPut]
        public async Task<ActionResult> UpdateMany(int quantity, CancellationToken cancellationToken)
        {
            var users = await _context.UserProfile.OrderBy(x => Guid.NewGuid()).Take(quantity).ToListAsync();
            foreach (var user in users)
            {
                var now = DateTime.UtcNow;
                user.Name = Faker.Name.FullName();
                user.Phone = Faker.Phone.Number();
                user.Email = Faker.Internet.Email();
                user.LastUpdatedTime = now;
            }

            var result = await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation($"{result} items have been updated");
            return Ok(users);
        }

        [HttpPut]
        public async Task<ActionResult> Update(Guid id, CancellationToken cancellationToken)
        {
            var user = await _context.UserProfile.FirstOrDefaultAsync(u => u.Id.Equals(id), cancellationToken);
            user.LastUpdatedTime = DateTime.UtcNow;
            user.Name = Faker.Name.FullName();
            user.Phone = Faker.Phone.Number();
            user.Email = Faker.Internet.Email();

            var result = await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation($"{result} items have been updated; Id = {user.Id}");
            return Ok(user);
        }

        [HttpPost]
        public async Task<ActionResult> Create(CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var user = new UserProfile
            {
                Id = Guid.NewGuid(),
                Name = Faker.Name.FullName(),
                Phone = Faker.Phone.Number(),
                Email = Faker.Internet.Email(),
                CreatedAt = now,
                LastUpdatedTime = now
            };
            _context.UserProfile.Add(user);
            var result = await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation($"{result} items have been created; Id = {user.Id}");
            return Ok(user);
        }

        [HttpGet]
        public async Task<ActionResult> Get(CancellationToken cancellationToken)
        {
            var users = await _context.UserProfile.AsNoTracking().ToListAsync(cancellationToken);
            return Ok(users);
        }

        [HttpGet]
        public async Task<ActionResult> User([Required] Guid id, CancellationToken cancellationToken)
        {
            var users = await _context.UserProfile.AsNoTracking().FirstOrDefaultAsync(u => u.Id.Equals(id), cancellationToken);
            return Ok(users);
        }

        [HttpDelete]
        public async Task<ActionResult> Delete(CancellationToken cancellationToken)
        {
            var result = await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE UserProfile;", cancellationToken);
            _logger.LogInformation($"Table {nameof(UserProfile)}'s data has been deleted");
            return Ok(result);
        }
    }
}
