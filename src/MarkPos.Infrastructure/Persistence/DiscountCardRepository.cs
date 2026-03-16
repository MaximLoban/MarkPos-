using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dapper;
using Microsoft.Data.SqlClient;
using MarkPos.Application.Interfaces;
using MarkPos.Domain.Entities;

namespace MarkPos.Infrastructure.Persistence;

public class DiscountCardRepository : IDiscountCardRepository
{
    private readonly string _connectionString;

    public DiscountCardRepository(string connectionString)
        => _connectionString = connectionString;

    public async Task<DiscountCard?> FindByNumberAsync(string cardNumber, CancellationToken ct = default)
    {
        const string sql = """
            SELECT TOP 1
                DiscountCardId,
                DiscountCardNumber  AS CardNumber,
                DiscountCardGroupId,
                DiscountCardGroupId2,
                DiscountCardTypeId
            FROM dbo.DiscountCard
            WHERE DiscountCardNumber = @CardNumber
        """;

        await using var conn = new SqlConnection(_connectionString);
        var row = await conn.QueryFirstOrDefaultAsync<DiscountCard>(
            new CommandDefinition(sql, new { CardNumber = cardNumber }, cancellationToken: ct));

        return row;
    }
}