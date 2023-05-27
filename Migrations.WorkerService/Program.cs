using Microsoft.EntityFrameworkCore;
using Migrations.WorkerService;
using Migrations.WorkerService.Data;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        IConfiguration config = hostContext.Configuration;
        services.AddHostedService<Worker>();
        services.AddDbContext<CloneUserContext>(option =>
        {
            option.UseSqlServer(config.GetConnectionString("TargetConnection"), providerOptions => providerOptions.CommandTimeout(120));
        });
        services.AddDbContext<UserContext>(option =>
        {
            option.UseSqlServer(config.GetConnectionString("SourceConnection"), providerOptions => providerOptions.CommandTimeout(120));
        });
    })
    .Build();
await host.RunAsync();
