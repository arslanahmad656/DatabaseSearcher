using DatabaseSearcher.Dto.Status;
using DatabaseSearcher.Dto;

namespace DatabaseSearcher.Contracts;

public interface IDbSearcher
{
    IAsyncEnumerable<SearchResult> Search(string text, IProgress<Status> progress, CancellationToken cancellationToken);

    IAsyncEnumerable<SearchResult> Search(string text, ICollection<string> tables, IProgress<Status> progress, CancellationToken cancellationToken);

    IAsyncEnumerable<SearchResult> SearchColumns(string text, IProgress<Status> progress, CancellationToken cancellationToken);

    IAsyncEnumerable<SearchResult> SearchColumns(string text, ICollection<string> tables, IProgress<Status> progress, CancellationToken cancellationToken);
}