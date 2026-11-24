using Mapster;
using ProductCatalogApp.Api.Dtos;
using ProductCatalogApp.Api.Models;
namespace ProductCatalogApp.Api.Mapping;

public class CatalogProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Equipment, EquipmentDto>()
            .MapToConstructor(true)
            .Map(dto => dto.ProductionSiteName, entity => entity.ProductionSite!.Name);
        config.NewConfig<EquipmentInput, Equipment>()
            .Ignore(entity => entity.Id)
            .Ignore(entity => entity.ProductionSite)
            .Map(entity => entity.Name, input => input.Name.Trim())
            .Map(entity => entity.Model, input => input.Model.Trim())
            .Map(entity => entity.InventoryNumber, input => input.InventoryNumber.Trim());
    }
}
