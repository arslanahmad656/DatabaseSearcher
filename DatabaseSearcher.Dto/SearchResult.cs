namespace DatabaseSearcher.Dto;

public record SearchResult(string Table, string Column, int RowNumber)
{
    public SearchResult(string table, CellLocation cellLocation)
        : this(table, cellLocation.Column, cellLocation.RowNumber)
    {

    }
}
