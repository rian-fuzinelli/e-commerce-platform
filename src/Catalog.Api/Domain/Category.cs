using Catalog.Api.Common;

namespace Catalog.Api.Domain;

public class Category
{
    // Construtor privado sem parâmetros: o EF Core precisa dele para materializar
    // a entidade vinda do banco. O null! diz ao compilador "confie, o EF preenche".
    private Category()
    {
        Name = null!;
        Slug = null!;
    }

    public Category(string name, string slug)
    {
        Id = Guid.CreateVersion7();
        Name = NormalizeName(name);
        Slug = NormalizeSlug(slug);
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private readonly List<Product> _products = [];
    public IReadOnlyCollection<Product> Products => _products.AsReadOnly();

    public void Rename(string name) => Name = NormalizeName(name);

    public void ChangeSlug(string slug) => Slug = NormalizeSlug(slug);

    private static string NormalizeName(string name) =>
        string.IsNullOrWhiteSpace(name)
            ? throw new DomainException("O nome da categoria é obrigatório.")
            : name.Trim();

    private static string NormalizeSlug(string slug) =>
        string.IsNullOrWhiteSpace(slug)
            ? throw new DomainException("O slug da categoria é obrigatório.")
            : slug.Trim().ToLowerInvariant();
}
