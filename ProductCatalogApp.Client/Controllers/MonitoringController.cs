using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using ProductCatalogApp.Client.Services;
namespace ProductCatalogApp.Client.Controllers;

public class MonitoringController(IProductCatalogApiClient api, IConfiguration configuration) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewData["HubUrl"] = (configuration["ApiSettings:PublicBaseUrl"] ?? configuration["ApiSettings:BaseUrl"])!.TrimEnd('/') + "/hubs/monitoring";
        ViewData["AccessToken"] = await HttpContext.GetTokenAsync("access_token");
        return View(await api.GetProductionSitesAsync());
    }
}
