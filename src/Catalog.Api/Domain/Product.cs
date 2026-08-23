using Catalog.Api.Common;

namespace Catalog.Api.Domain;

/// <summary>
/// Entidade rica: os dados só mudam por métodos que garantem as invariantes.
/// Se as propriedades tivessem set público, qualquer camada poderia gravar
/// um preço negativo — e a regra deixaria de existir.
/// </summary>
public class Product
{
    private Product()
    {
        Sku = null!;
        Name = null!;
        Currency = null!;
    }

    public Product(
        string sku,
        string name,
        string? description,
        decimal price,
        string currency,
        int stockQuantity,
        Guid categoryId)
    {
        // Guid v7 é sequencial no tempo: evita fragmentação de índice no PostgreSQL,
        // problema clássico de usar Guid.NewGuid() como chave primária.
        Id = Guid.CreateVersion7();
        Sku = NormalizeSku(sku);
        Name = NormalizeName(name);
        Description = NormalizeDescription(description);
        Price = NormalizePrice(price);
        Currency = NormalizeCurrency(currency);
        StockQuantity = EnsureNonNegative(stockQuantity);
        CategoryId = EnsureCategory(categoryId);
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Sku { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public string Currency { get; private set; }
    public int StockQuantity { get; private set; }
    public bool IsActive { get; private set; }
    public Guid CategoryId { get; private set; }
    public Category? Category { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public void Rename(string name)
    {
        Name = NormalizeName(name);
        Touch();
    }

    public void ChangeDescription(string? description)
    {
        Description = NormalizeDescription(description);
        Touch();
    }

    public void ChangePrice(decimal price, string currency)
    {
        Price = NormalizePrice(price);
        Currency = NormalizeCurrency(currency);
        Touch();
    }

    public void SetStock(int quantity)
    {
        StockQuantity = EnsureNonNegative(quantity);
        Touch();
    }

    public void DecreaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("A quantidade a baixar deve ser positiva.");

        if (quantity > StockQuantity)
            throw new DomainException($"Estoque insuficiente. Disponível: {StockQuantity}.");

        StockQuantity -= quantity;
        Touch();
    }

    public void MoveToCategory(Guid categoryId)
    {
        CategoryId = EnsureCategory(categoryId);
        Touch();
    }

    public void Activate()
    {
        IsActive = true;
        Touch();
    }

    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;

    private static string NormalizeSku(string sku) =>
        string.IsNullOrWhiteSpace(sku)
            ? throw new DomainException("O SKU é obrigatório.")
            : sku.Trim().ToUpperInvariant();

    private static string NormalizeName(string name) =>
        string.IsNullOrWhiteSpace(name)
            ? throw new DomainException("O nome do produto é obrigatório.")
            : name.Trim();

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private static decimal NormalizePrice(decimal price) =>
        price <= 0
            ? throw new DomainException("O preço deve ser maior que zero.")
            : decimal.Round(price, 2, MidpointRounding.ToEven);

    private static string NormalizeCurrency(string currency) =>
        string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3
            ? throw new DomainException("A moeda deve seguir o padrão ISO 4217 (3 letras), ex.: BRL.")
            : currency.Trim().ToUpperInvariant();

    private static int EnsureNonNegative(int quantity) =>
        quantity < 0
            ? throw new DomainException("O estoque não pode ser negativo.")
            : quantity;

    private static Guid EnsureCategory(Guid categoryId) =>
        categoryId == Guid.Empty
            ? throw new DomainException("A categoria do produto é obrigatória.")
            : categoryId;
}
