using Microsoft.EntityFrameworkCore;
using Migrations.WorkerService.Data;
using Migrations.WorkerService.Models;
using Migrations.WorkerService.Services.Implements;
using Migrations.WorkerService.Services.Interfaces;
using System.Linq;

namespace Migrations.WorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDatabaseService _databaseService;

        public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider, IDatabaseService databaseService)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _databaseService = databaseService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation($"Worker running at: {DateTimeOffset.Now}");

                using var scope = _serviceProvider.CreateScope();
                var cloneUserContext = scope.ServiceProvider.GetService<CloneUserContext>();
                var userContext = scope.ServiceProvider.GetService<UserContext>();
                var dbService = scope.ServiceProvider.GetService<DatabaseService>();
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

                        if (srcUsers.Count >= 1000)
                        {
                            var cloneUsers = srcUsers.Select(x => new UserProfile
                            {
                                Id = x.Id,
                                Name = x.Name,
                                CreatedAt = x.CreatedAt,
                                LastUpdatedTime = x.LastUpdatedTime,
                                Email = x.Email,
                                Phone = x.Phone,
                                RowVersion = x.RowVersion
                            }).ToList();

                            var datatable = _databaseService.ConvertListToDatatable(cloneUsers);
                            await _databaseService.ExecuteMergeDataAsync(datatable, nameof(UserProfile), stoppingToken);
                            _logger.LogInformation($"{cloneUsers.Count} items have been migrated");
                           
                        }
                        else if (srcUsers.Count >  0 && srcUsers.Count < 1000)
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
                        else
                        {
                            _logger.LogInformation("No data needs to migrated");
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Get data from source");
                        var srcUsers = await userContext.UserProfile.AsNoTracking().ToListAsync(stoppingToken);

                        if (srcUsers.Count >= 1000)
                        {
                            var datatable = _databaseService.ConvertListToDatatable(srcUsers);
                            await _databaseService.ExecuteBulkCopyAsync(datatable, nameof(UserProfile), stoppingToken);
                            _logger.LogInformation($"{srcUsers.Count} items have been migrated");
                        }
                        if (srcUsers.Count > 0 && srcUsers.Count < 1000)
                        {
                            cloneUserContext.UserProfile.AddRange(srcUsers);
                            var result = await cloneUserContext.SaveChangesAsync(stoppingToken);
                            await transaction.CommitAsync(stoppingToken);
                            _logger.LogInformation($"{result} items have been migrated");
                        }
                        else
                        {
                            _logger.LogInformation("Table is empty");
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