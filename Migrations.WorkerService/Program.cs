using Microsoft.EntityFrameworkCore;
using Migrations.WorkerService;
using Migrations.WorkerService.Data;
using Migrations.WorkerService.Services.Implements;
using Migrations.WorkerService.Services.Interfaces;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        IConfiguration config = hostContext.Configuration;
        services.AddDbContext<CloneUserContext>(option =>
        {
            option.UseSqlServer(config.GetConnectionString("TargetConnection"), providerOptions => providerOptions.CommandTimeout(120));
        });
        services.AddDbContext<UserContext>(option =>
        {
            option.UseSqlServer(config.GetConnectionString("SourceConnection"), providerOptions => providerOptions.CommandTimeout(120));
        });

       services.AddTransient<IDatabaseService, DatabaseService>();

        services.AddHostedService<Worker>();
        
    })
    .Build();
await host.RunAsync();
