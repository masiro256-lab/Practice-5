using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ProductCatalogApp.Api.Data;
using ProductCatalogApp.Api.Services;
namespace ProductCatalogApp.Api.Hubs;

public class OnlineConnections
{
    private readonly ConcurrentDictionary<string, byte> connections = new();
    public int Add(string id) { connections.TryAdd(id, 0); return connections.Count; }
    public int Remove(string id) { connections.TryRemove(id, out _); return connections.Count; }
}

public class MonitoringHub(ReadingService readings, ApplicationDbContext db, OnlineConnections online) : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Clients.All.SendAsync("OnlineChanged", online.Add(Context.ConnectionId));
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Clients.All.SendAsync("OnlineChanged", online.Remove(Context.ConnectionId));
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinSite(int? siteId)
    {
        if (siteId.HasValue && !await db.ProductionSites.AnyAsync(s => s.Id == siteId))
            throw new HubException("Участок не найден.");
        var group = siteId.HasValue ? "site:" + siteId.Value : "all-sites";
        if (Context.Items.TryGetValue("group", out var previous) && previous is string old)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, old);
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
        Context.Items["group"] = group;
        await Clients.Caller.SendAsync("ReceiveSnapshot", await readings.SnapshotAsync(siteId));
    }

    public async Task SubmitReading(int sensorId, double value, string source)
    {
        if (!double.IsFinite(value) || Math.Abs(value) > 1_000_000)
            throw new HubException("Некорректное значение замера.");
        if (string.IsNullOrWhiteSpace(source) || source.Length > 64)
            throw new HubException("Укажите источник замера длиной до 64 символов.");
        var reading = await readings.AddAsync(sensorId, value, source.Trim());
        if (reading == null) throw new HubException("Датчик не найден.");
        await Clients.Groups("all-sites", "site:" + reading.ProductionSiteId).SendAsync("ReadingReceived", reading);
        if (reading.IsAlarm) await Clients.All.SendAsync("AlarmRaised", reading);
        await Clients.Caller.SendAsync("ReadingAccepted", reading.Id);
    }
}
