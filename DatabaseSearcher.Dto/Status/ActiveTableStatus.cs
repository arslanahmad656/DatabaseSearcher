namespace DatabaseSearcher.Dto.Status;

public record ActiveTableStatus(string TableName, int TotalRows, int RowsProcessed);