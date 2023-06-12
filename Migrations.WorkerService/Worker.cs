using Microsoft.EntityFrameworkCore;
using Migrations.WorkerService.Data;
using Migrations.WorkerService.Models;
using System.Linq;

namespace Migrations.WorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _serviceProvider;

        public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation($"Worker running at: {DateTimeOffset.Now}");

                using var scope = _serviceProvider.CreateScope();
                var cloneUserContext = scope.ServiceProvider.GetService<CloneUserContext>();
                var userContext = scope.ServiceProvider.GetService<UserContext>();
                using var transaction = cloneUserContext.Database.BeginTransaction();

                try
                {
                    var latestUser = await cloneUserContext.UserProfile.OrderByDescending(u => u.LastUpdatedTime).Select(x =>
                    new UserProfile
                    {
                        Id = x.Id,
                        LastUpdatedTime = x.LastUpdatedTime
                    }).FirstOrDefaultAsync(stoppingToken);

                    if (latestUser != null)
                    {
                        _logger.LogInformation($"Last update time: {latestUser.LastUpdatedTime}");

                        var srcUsers = await userContext.UserProfile.AsNoTracking()
                            .Where(u => u.LastUpdatedTime > latestUser.LastUpdatedTime)
                            .ToListAsync(stoppingToken);

                        if (srcUsers.Any())
                        {
                            var srcUserIds = srcUsers.Select(x => x.Id);
                            var srcUserRowVesrions = srcUsers.Select(x => x.RowVersion);

                            var needUpdateUsers = await cloneUserContext.UserProfile
                                .Where(u => srcUserIds.Contains(u.Id) && !srcUserRowVesrions.Contains(u.RowVersion))
                                .Select(x => new UserProfile { Id = x.Id })
                                .ToListAsync(stoppingToken);

                            var needUpdateUserIds = needUpdateUsers.Select(x => x.Id);

                            var needInsertUsers = srcUsers.Where(x => !needUpdateUserIds.Contains(x.Id));

                            cloneUserContext.UserProfile.AddRange(needInsertUsers);

                            needUpdateUsers = srcUsers.Where(x => needUpdateUserIds.Contains(x.Id))
                                .Select(x => new UserProfile
                                {
                                    Id = x.Id,
                                    Name = x.Name,
                                    CreatedAt = x.CreatedAt,
                                    LastUpdatedTime = x.LastUpdatedTime,
                                    Email = x.Email,
                                    Phone = x.Phone,
                                    RowVersion = x.RowVersion
                                }).ToList();

                            cloneUserContext.UserProfile.UpdateRange(needUpdateUsers);

                            var result = await cloneUserContext.SaveChangesAsync(stoppingToken);

                            await transaction.CommitAsync(stoppingToken);

                            _logger.LogInformation($"{result} items have been migrated");
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Get data from source");
                        var srcUsers = await userContext.UserProfile.AsNoTracking().ToListAsync(stoppingToken);
                        if (srcUsers.Any())
                        {
                            cloneUserContext.UserProfile.AddRange(srcUsers);
                            var result = await cloneUserContext.SaveChangesAsync(stoppingToken);
                            await transaction.CommitAsync(stoppingToken);
                            _logger.LogInformation($"{result} items have been migrated");
                        }
                        else
                        {
                            _logger.LogInformation($"No items found");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    _logger.LogInformation("Rollback transaction");
                    await transaction.RollbackAsync(stoppingToken);
                    throw ex;
                }
                await Task.Delay(10 * 1000, stoppingToken);
            }
        }
    }
}