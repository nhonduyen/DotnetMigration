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

            await _context.UserProfile.AddRangeAsync(users, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
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

            await _context.SaveChangesAsync(cancellationToken);
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

            await _context.SaveChangesAsync(cancellationToken);
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
            await _context.UserProfile.AddAsync(user, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
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
            var users = await _context.UserProfile.Select(x => new UserProfile { Id = x.Id }).ToListAsync(cancellationToken);
            _context.UserProfile.RemoveRange(users);
            var result = await _context.SaveChangesAsync(cancellationToken);
            return Ok(result);
        }
    }
}
