using Dapper;
using Microsoft.Data.SqlClient;
using MarkPos.Application.Interfaces;

namespace MarkPos.Infrastructure.Persistence;

public class CatalogRepository : ICatalogRepository
{
    private readonly string _connectionString;

    public CatalogRepository(string connectionString)
        => _connectionString = connectionString;

    private class GroupRow { public long GoodsWeightGroupId; public long DepartmentNumber; public string Name = ""; }
    private class ProductRow { public long GoodsId; public string Name = ""; public decimal Price; }

    public async Task<IReadOnlyList<GoodsGroupItem>> GetParentGroupsAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT g.GoodsWeightGroupId, g.DepartmentNumber, g.GoodsWeightGroupName AS Name
            FROM dbo.GoodsWeightGroup g
            WHERE g.DepartmentNumber BETWEEN 1000 AND 1500
              AND EXISTS (SELECT 1 FROM dbo.GoodsWeightGroupLink l WHERE l.GoodsWeightGroupParentId = g.GoodsWeightGroupId)
            ORDER BY g.DepartmentNumber
            """;

        await using var conn = new SqlConnection(_connectionString);
        var rows = await conn.QueryAsync<GroupRow>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.Select(r => new GoodsGroupItem(r.GoodsWeightGroupId, r.DepartmentNumber, r.Name)).AsList();
    }

    public async Task<IReadOnlyList<GoodsGroupItem>> GetChildGroupsAsync(long parentGroupId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT g.GoodsWeightGroupId, g.DepartmentNumber, g.GoodsWeightGroupName AS Name
            FROM dbo.GoodsWeightGroupLink l
            JOIN dbo.GoodsWeightGroup g ON g.GoodsWeightGroupId = l.GoodsWeightGroupId
            WHERE l.GoodsWeightGroupParentId = @ParentGroupId
            ORDER BY g.DepartmentNumber
            """;

        await using var conn = new SqlConnection(_connectionString);
        var rows = await conn.QueryAsync<GroupRow>(new CommandDefinition(sql, new { ParentGroupId = parentGroupId }, cancellationToken: ct));
        return rows.Select(r => new GoodsGroupItem(r.GoodsWeightGroupId, r.DepartmentNumber, r.Name)).AsList();
    }

    public async Task<bool> HasChildrenAsync(long groupId, CancellationToken ct = default)
    {
        const string sql = "SELECT COUNT(1) FROM dbo.GoodsWeightGroupLink WHERE GoodsWeightGroupParentId = @GroupId";

        await using var conn = new SqlConnection(_connectionString);
        var count = await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { GroupId = groupId }, cancellationToken: ct));
        return count > 0;
    }

    public async Task<IReadOnlyList<CatalogProductItem>> GetGroupItemsAsync(long groupId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT gw.GoodsId, g.GoodsName AS Name, g.Price
            FROM dbo.GoodsWeight gw
            JOIN dbo.Goods g ON g.GoodsId = gw.GoodsId
            WHERE gw.GoodsWeightGroupId = @GroupId
            ORDER BY g.GoodsName
            """;

        await using var conn = new SqlConnection(_connectionString);
        var rows = await conn.QueryAsync<ProductRow>(new CommandDefinition(sql, new { GroupId = groupId }, cancellationToken: ct));
        return rows.Select(r => new CatalogProductItem(r.GoodsId, r.Name, r.Price)).AsList();
    }
}
