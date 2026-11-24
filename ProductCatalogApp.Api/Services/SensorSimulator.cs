using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ProductCatalogApp.Api.Data;
using ProductCatalogApp.Api.Hubs;
namespace ProductCatalogApp.Api.Services;

public class SensorSimulator(IServiceScopeFactory scopes, IHubContext<MonitoringHub> hub,
    IConfiguration configuration, ILogger<SensorSimulator> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue("Monitoring:SimulationEnabled", true)) return;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = scopes.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var readings = scope.ServiceProvider.GetRequiredService<ReadingService>();
                    var sensors = await db.Sensors.AsNoTracking().ToListAsync(stoppingToken);
                    foreach (var sensor in sensors)
                    {
                        var range = sensor.MaxValue - sensor.MinValue;
                        var value = sensor.MinValue + range * (Random.Shared.NextDouble() * 1.4 - 0.2);
                        var reading = await readings.AddAsync(sensor.Id, value, "Имитатор");
                        if (reading == null) continue;
                        await hub.Clients.Groups("all-sites", "site:" + reading.ProductionSiteId).SendAsync("ReadingReceived", reading, stoppingToken);
                        if (reading.IsAlarm) await hub.Clients.All.SendAsync("AlarmRaised", reading, stoppingToken);
                    }
                }
                catch (Exception e) when (e is not OperationCanceledException) { logger.LogError(e, "Ошибка имитации показаний"); }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
