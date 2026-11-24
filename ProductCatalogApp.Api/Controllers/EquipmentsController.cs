using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductCatalogApp.Api.Data;
using ProductCatalogApp.Api.Dtos;
using ProductCatalogApp.Api.Models;
namespace ProductCatalogApp.Api.Controllers;

[ApiController, Authorize, Route("api/[controller]")]
public class EquipmentsController(ApplicationDbContext db, IMapper mapper, TypeAdapterConfig mapping) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<EquipmentDto>>> GetAll([FromQuery] int? productionSiteId, [FromQuery] string? search)
    {
        var query = db.EquipmentItems.AsNoTracking();
        if (productionSiteId.HasValue) query = query.Where(e => e.ProductionSiteId == productionSiteId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e => e.Name.Contains(term) || e.Model.Contains(term) || e.InventoryNumber.Contains(term));
        }
        return Ok(await query.OrderBy(e => e.Name).ProjectToType<EquipmentDto>(mapping).ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EquipmentDto>> GetById(int id)
    {
        var dto = await db.EquipmentItems.AsNoTracking().Where(e => e.Id == id).ProjectToType<EquipmentDto>(mapping).FirstOrDefaultAsync();
        return dto == null ? NotFound() : Ok(dto);
    }

    [HttpPost, Authorize(Roles = "Admin")]
    public async Task<ActionResult<EquipmentDto>> Create(EquipmentInput input)
    {
        if (!await ValidSite(input.ProductionSiteId)) return ValidationProblem(ModelState);
        var entity = mapper.Map<Equipment>(input);
        db.EquipmentItems.Add(entity);
        await db.SaveChangesAsync();
        var dto = await db.EquipmentItems.Where(e => e.Id == entity.Id).ProjectToType<EquipmentDto>(mapping).SingleAsync();
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    [HttpPut("{id:int}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, EquipmentInput input)
    {
        var entity = await db.EquipmentItems.FindAsync(id);
        if (entity == null) return NotFound();
        if (!await ValidSite(input.ProductionSiteId)) return ValidationProblem(ModelState);
        mapper.Map(input, entity);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await db.EquipmentItems.FindAsync(id);
        if (entity == null) return NotFound();
        db.EquipmentItems.Remove(entity);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<bool> ValidSite(int id)
    {
        if (await db.ProductionSites.AnyAsync(s => s.Id == id)) return true;
        ModelState.AddModelError(nameof(EquipmentInput.ProductionSiteId), "Выберите существующий участок.");
        return false;
    }
}
