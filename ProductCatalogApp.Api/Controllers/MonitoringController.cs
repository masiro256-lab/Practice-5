using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductCatalogApp.Api.Dtos;
using ProductCatalogApp.Api.Services;
namespace ProductCatalogApp.Api.Controllers;

[Authorize]
[ApiController, Route("api/monitoring")]
public class MonitoringController(ReadingService readings) : ControllerBase
{
    [HttpGet("snapshot")]
    public Task<MonitoringSnapshot> Snapshot([FromQuery] int? siteId) => readings.SnapshotAsync(siteId);
}
