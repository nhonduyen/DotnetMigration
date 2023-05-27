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
                try
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                    using var scope = _serviceProvider.CreateScope();
                    var cloneUserContext = scope.ServiceProvider.GetService<CloneUserContext>();
                    var userContext = scope.ServiceProvider.GetService<UserContext>();

                    var latestUser = await cloneUserContext.UserProfile.OrderByDescending(u => u.LastUpdatedTime).Select(x =>
                    new UserProfile
                    {
                        Id = x.Id,
                        LastUpdatedTime = x.LastUpdatedTime
                    }).FirstOrDefaultAsync(stoppingToken);

                    if (latestUser != null)
                    {
                        _logger.LogInformation("Lastest time: {time}", latestUser.LastUpdatedTime);
                        var srcUsers = await userContext.UserProfile.AsNoTracking().Where(u => u.LastUpdatedTime > latestUser.LastUpdatedTime).ToListAsync(stoppingToken);
                        if (srcUsers.Any())
                        {
                            var srcUserIds = srcUsers.Select(x => x.Id);
                            var srcUserRowVesrions = srcUsers.Select(x => x.RowVersion);

                            var needUpdateUserIds = await cloneUserContext.UserProfile.Where(u => srcUserIds.Contains(u.Id) && !srcUserRowVesrions.Contains(u.RowVersion)).Select(x => x.Id).ToListAsync(stoppingToken);
                            var needInsertUsers = srcUsers.Where(x => !needUpdateUserIds.Contains(x.Id));

                            await cloneUserContext.UserProfile.AddRangeAsync(needInsertUsers, stoppingToken);

                            var needUpdateUsers = srcUsers.Where(x => needUpdateUserIds.Contains(x.Id));
                            cloneUserContext.UserProfile.UpdateRange(needUpdateUsers);

                            var result = await cloneUserContext.SaveChangesAsync(stoppingToken);

                            _logger.LogInformation($"{result} items have been migrated");
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Get data from source");
                        var srcUsers = await userContext.UserProfile.AsNoTracking().ToListAsync(stoppingToken);
                        await cloneUserContext.UserProfile.AddRangeAsync(srcUsers.Cast<UserProfile>(), stoppingToken);
                        var result = await cloneUserContext.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation($"{result} items have been migrated");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    throw ex;
                }
                await Task.Delay(10 * 1000, stoppingToken);
            }
        }
    }
}