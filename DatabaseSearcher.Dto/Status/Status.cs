namespace DatabaseSearcher.Dto.Status;

public record Status(double PercentageProcessed, TotalTablesStatus TotalTablesStatus, ActiveTableStatus ActiveTableStatus);

