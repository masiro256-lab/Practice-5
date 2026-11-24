using Microsoft.EntityFrameworkCore;
using ProductCatalogApp.Api.Models;
namespace ProductCatalogApp.Api.Data;

public static class MonitoringSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        if (await db.Sensors.AnyAsync()) return;
        var equipment = await db.EquipmentItems.OrderBy(e => e.Id).Take(3).ToListAsync();
        foreach (var item in equipment)
            db.Sensors.Add(new Sensor { Name = "Температура — " + item.Name, Unit = "°C", MinValue = 100, MaxValue = 300, EquipmentId = item.Id });
        await db.SaveChangesAsync();
    }
}
