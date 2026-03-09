using Xunit;
using MarkPos.Infrastructure.Persistence;

namespace MarkPos.Infrastructure.Tests;

public class ProductRepositoryTests
{
    private const string ConnectionString =
        "Server=localhost;Database=MarkPos;Trusted_Connection=True;TrustServerCertificate=True";

    [Fact]
    public async Task FindByBarcode_ExistingBarcode_ReturnsProduct()
    {
        var repo = new ProductRepository(ConnectionString);

        // Возьмите любой реальный штрихкод из вашей БД
        var product = await repo.FindByBarcodeAsync("4810557003449");

        Assert.NotNull(product);
        Assert.True(product.GoodsId > 0);
        Assert.False(string.IsNullOrEmpty(product.Name));
    }

    [Fact]
    public async Task FindByBarcode_UnknownBarcode_ReturnsNull()
    {
        var repo = new ProductRepository(ConnectionString);

        var product = await repo.FindByBarcodeAsync("0000000000000");

        Assert.Null(product);
    }

    [Fact]
    public async Task Search_ValidQuery_ReturnsResults()
    {
        var repo = new ProductRepository(ConnectionString);

        // Возьмите любое слово которое точно есть в названиях товаров
        var results = await repo.SearchAsync("сардельки", limit: 5);

        Assert.NotEmpty(results);
        Assert.True(results.Count <= 5);
    }
}