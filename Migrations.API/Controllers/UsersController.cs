﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Migrations.API.Data;
using Migrations.API.Models;
using Faker;
using System.ComponentModel.DataAnnotations;
using Migrations.API.Services.Interfaces;

namespace Migrations.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UserContext _context;
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(UserContext context, IDatabaseService databaseService, ILogger<UsersController> logger)
        {
            _context = context;
            _databaseService = databaseService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult> CreateMany(int quantity, CancellationToken cancellationToken)
        {
            var users = new List<UserProfile>(quantity);
            for (int i = 0; i < quantity; i++)
            {
                var user = new UserProfile
                {
                    Id = Guid.NewGuid(),
                    Name = Faker.Name.FullName(),
                    Phone = Faker.Phone.Number(),
                    Email = Faker.Internet.Email(),
                    CreatedAt = DateTime.UtcNow,
                    LastUpdatedTime = DateTime.UtcNow
                };
                users.Add(user);
            }

            var result = 0;

            if (quantity < 1000)
            {
                _context.UserProfile.AddRange(users);
                result = await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation($"{result} items have been created");
            }
            else
            {
                var datatable = _databaseService.ConvertListToDatatable(users);
                await _databaseService.ExecuteBulkCopyAsync(datatable, users.FirstOrDefault(), cancellationToken);
                result = quantity;
                _logger.LogInformation($"Bulk copy: {result} items have been created");
            }
            
            return Ok(result);
        }

        [HttpPut]
        public async Task<ActionResult> UpdateMany(int quantity, CancellationToken cancellationToken)
        {
            var users = await _context.UserProfile.OrderBy(x => Guid.NewGuid()).Take(quantity).ToListAsync(cancellationToken);
            foreach (var user in users)
            {
                user.Name = Faker.Name.FullName();
                user.Phone = Faker.Phone.Number();
                user.Email = Faker.Internet.Email();
            }

            _context.UserProfile.UpdateRange(users);
            var result = await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation($"{result} items have been updated");
            return Ok(result);
        }

        [HttpPut]
        public async Task<ActionResult> Update(Guid id, CancellationToken cancellationToken)
        {
            var user = await _context.UserProfile.FirstOrDefaultAsync(u => u.Id.Equals(id), cancellationToken);
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
            var user = new UserProfile
            {
                Name = Faker.Name.FullName(),
                Phone = Faker.Phone.Number(),
                Email = Faker.Internet.Email()
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
