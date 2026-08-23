namespace Catalog.Api.Contracts.Common;

public static class PageRequest
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    /// <summary>
    /// Normaliza page/pageSize vindos da query string.
    /// Limitar o pageSize é defesa contra um cliente pedir 1.000.000 de linhas.
    /// </summary>
    public static (int Page, int PageSize) Normalize(int? page, int? pageSize)
    {
        var normalizedPage = page is null or < 1 ? 1 : page.Value;
        var normalizedSize = pageSize switch
        {
            null or < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => pageSize.Value
        };

        return (normalizedPage, normalizedSize);
    }
}
