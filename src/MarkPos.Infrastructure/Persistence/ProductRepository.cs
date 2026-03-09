using Dapper;
using Microsoft.Data.SqlClient;
using MarkPos.Application.Interfaces;
using MarkPos.Domain.Entities;

namespace MarkPos.Infrastructure.Persistence;

public class ProductRepository : IProductRepository
{
    private readonly string _connectionString;

    public ProductRepository(string connectionString)
        => _connectionString = connectionString;

    public async Task<Product?> FindByBarcodeAsync(string barcode, CancellationToken ct = default)
    {
        const string sql = """
            SELECT TOP 1
                g.GoodsId,
                g.GoodsGroupId,
                g.GoodsName        AS Name,
                g.Price,
                g.DiscountGroupId,
                g.GoodsMinQuantity,
                g.Piece,
                g.GoodsTypeForFiscal,
                g.MarkingType,
                g.Gtin,
                b.Barcode,
                b.Quantity         AS BarcodeQuantity
            FROM dbo.Barcodes b
            JOIN dbo.Goods g ON g.GoodsId = b.GoodsId
            WHERE b.Barcode = @Barcode
        """;

        await using var conn = new SqlConnection(_connectionString);
        var row = await conn.QueryFirstOrDefaultAsync<ProductRow>(
            new CommandDefinition(sql, new { Barcode = barcode }, cancellationToken: ct));

        return row == null ? null : MapProduct(row);
    }

    public async Task<IReadOnlyList<Product>> SearchAsync(
        string query, int limit = 20, CancellationToken ct = default)
    {
        const string sql = """
            SELECT TOP (@Limit)
                g.GoodsId,
                g.GoodsGroupId,
                g.GoodsName        AS Name,
                g.Price,
                g.DiscountGroupId,
                g.GoodsMinQuantity,
                g.Piece,
                g.GoodsTypeForFiscal,
                g.MarkingType,
                g.Gtin,
                b.Barcode,
                b.Quantity         AS BarcodeQuantity
            FROM dbo.Goods g
            LEFT JOIN dbo.Barcodes b ON b.GoodsId = g.GoodsId
            WHERE g.GoodsName LIKE @Query
            ORDER BY g.GoodsName
        """;

        await using var conn = new SqlConnection(_connectionString);
        var rows = await conn.QueryAsync<ProductRow>(
            new CommandDefinition(sql, new { Query = $"%{query}%", Limit = limit },
            cancellationToken: ct));

        return rows.Select(MapProduct).ToList();
    }

    private static Product MapProduct(ProductRow r) => new()
    {
        GoodsId = r.GoodsId,
        GoodsGroupId = r.GoodsGroupId,
        Name = r.Name,
        Price = r.Price,
        Barcode = r.Barcode,
        Gtin = r.Gtin,
        BarcodeQuantity = r.BarcodeQuantity,
        DiscountGroupId = r.DiscountGroupId,
        GoodsMinQuantity = r.GoodsMinQuantity,
        Piece = r.Piece,
        GoodsTypeForFiscal = r.GoodsTypeForFiscal,
        MarkingType = r.MarkingType
    };

    // Приватный класс для маппинга Dapper
    private class ProductRow
    {
        public long GoodsId { get; init; }
        public long GoodsGroupId { get; init; }
        public string Name { get; init; } = string.Empty;
        public decimal Price { get; init; }
        public string? Barcode { get; init; }
        public string? Gtin { get; init; }
        public decimal? BarcodeQuantity { get; init; }
        public long? DiscountGroupId { get; init; }
        public decimal? GoodsMinQuantity { get; init; }
        public long? Piece { get; init; }
        public long? GoodsTypeForFiscal { get; init; }
        public short? MarkingType { get; init; }
    }
}