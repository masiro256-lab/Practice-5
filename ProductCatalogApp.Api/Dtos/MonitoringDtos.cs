namespace ProductCatalogApp.Api.Dtos;

public record SensorDto(int Id, string Name, string Unit, double MinValue, double MaxValue, int ProductionSiteId, string EquipmentName);
public record ReadingDto(int Id, int SensorId, string SensorName, string Unit, int ProductionSiteId, double Value, DateTime Timestamp, string Source, bool IsAlarm);
public record MonitoringSnapshot(List<SensorDto> Sensors, List<ReadingDto> Readings);
