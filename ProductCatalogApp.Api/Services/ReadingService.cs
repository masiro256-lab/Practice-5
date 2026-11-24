using Microsoft.EntityFrameworkCore;
using ProductCatalogApp.Api.Data;
using ProductCatalogApp.Api.Dtos;
using ProductCatalogApp.Api.Models;
namespace ProductCatalogApp.Api.Services;

public class ReadingService(ApplicationDbContext db)
{
    public async Task<MonitoringSnapshot> SnapshotAsync(int? siteId)
    {
        var sensors = db.Sensors.AsNoTracking().AsQueryable();
        var readings = db.SensorReadings.AsNoTracking();
        if (siteId.HasValue)
        {
            sensors = sensors.Where(s => s.Equipment!.ProductionSiteId == siteId);
            readings = readings.Where(r => r.Sensor!.Equipment!.ProductionSiteId == siteId);
        }
        return new MonitoringSnapshot(
            await sensors.OrderBy(s => s.Id).Select(s => new SensorDto(s.Id, s.Name, s.Unit, s.MinValue, s.MaxValue, s.Equipment!.ProductionSiteId, s.Equipment.Name)).ToListAsync(),
            await readings.OrderByDescending(r => r.Id).Take(50).Select(r =>
                new ReadingDto(r.Id, r.SensorId, r.Sensor!.Name, r.Sensor.Unit,
                    r.Sensor.Equipment!.ProductionSiteId, r.Value,
                    DateTime.SpecifyKind(r.Timestamp, DateTimeKind.Utc), r.Source, r.IsAlarm)).ToListAsync());
    }

    public async Task<ReadingDto?> AddAsync(int sensorId, double value, string source)
    {
        var sensor = await db.Sensors.Include(s => s.Equipment).FirstOrDefaultAsync(s => s.Id == sensorId);
        if (sensor == null) return null;
        var reading = new SensorReading
        {
            SensorId = sensor.Id, Value = Math.Round(value, 2), Timestamp = DateTime.UtcNow,
            Source = source, IsAlarm = sensor.IsOutOfRange(value)
        };
        db.SensorReadings.Add(reading);
        await db.SaveChangesAsync();
        return new ReadingDto(reading.Id, sensor.Id, sensor.Name, sensor.Unit, sensor.Equipment!.ProductionSiteId,
            reading.Value, reading.Timestamp, reading.Source, reading.IsAlarm);
    }
}
