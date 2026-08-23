namespace Catalog.Api.Contracts.Common;

/// <summary>
/// Envelope de paginação. Nunca devolva uma coleção "crua" em um endpoint de listagem:
/// sem envelope o cliente não sabe se existe próxima página nem o total.
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long TotalItems)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}
