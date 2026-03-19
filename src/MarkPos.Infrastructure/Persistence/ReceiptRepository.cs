using Dapper;
using MarkPos.Application.Interfaces;
using MarkPos.Domain.Entities;
using Microsoft.Data.SqlClient;

namespace MarkPos.Infrastructure.Persistence;

public class ReceiptRepository : IReceiptRepository
{
    private readonly string _connectionString;

    public ReceiptRepository(string connectionString)
        => _connectionString = connectionString;

    public async Task<long> CreateGroupAsync(Receipt receipt, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO dbo.CreditGroup
            (
                Datex, Checkx, IsReturn, StationNumber,
                CreditGroupTime, DiscountCardId, DiscountCardGroupId,
                DiscountCardGroupId2, DiscountSum, FiscalType,
                CreditGroupGUID, RetailSaleTypeId, WhatBase
            )
            VALUES
            (
                @Datex, @Checkx, 0, @StationNumber,
                @Datex, @DiscountCardId, @DiscountCardGroupId,
                @DiscountCardGroupId2, 0, @FiscalType,
                NEWID(), @RetailSaleTypeId, 11
            );
            SELECT CAST(SCOPE_IDENTITY() AS bigint);
            """;

        await using var conn = new SqlConnection(_connectionString);
        var id = await conn.ExecuteScalarAsync<long>(
            new CommandDefinition(sql, new
            {
                Datex = receipt.CreatedAt,
                Checkx = receipt.Id,
                StationNumber = receipt.StationNumber,
                DiscountCardId = receipt.DiscountCard?.DiscountCardId ?? 0,
                DiscountCardGroupId = receipt.DiscountCard?.DiscountCardGroupId ?? 0,
                DiscountCardGroupId2 = receipt.DiscountCard?.DiscountCardGroupId2 ?? 0,
                FiscalType = receipt.FiscalType,
                RetailSaleTypeId = receipt.RetailSaleTypeId
            }, cancellationToken: ct));

        return id;
    }

    public async Task InsertLineAsync(long creditGroupId, ReceiptLine line, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO dbo.Credit
            (
                CreditGroupId, GoodsId, Price, PriceOld, Quantity, Summa,
                LineNumber, CreditIsReturn, DiscountGroupId, GoodsMinQuantity,
                StartTime, DiscountSum, GoodsTypeForFiscal, Barcode
            )
            VALUES
            (
                @CreditGroupId, @GoodsId, @Price, @PriceOld, @Quantity, @Summa,
                @LineNumber, 0, @DiscountGroupId, @GoodsMinQuantity,
                @StartTime, @DiscountSum, @GoodsTypeForFiscal, @Barcode
            );
            """;

        await using var conn = new SqlConnection(_connectionString);
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            CreditGroupId = creditGroupId,
            GoodsId = line.Product.GoodsId,
            Price = line.Price,
            PriceOld = line.PriceOld,
            Quantity = line.Quantity,
            Summa = line.TotalSum,
            LineNumber = line.LineNumber,
            DiscountGroupId = line.Product.DiscountGroupId,
            GoodsMinQuantity = line.Product.GoodsMinQuantity,
            StartTime = DateTime.Now,
            DiscountSum = line.DiscountSum,
            GoodsTypeForFiscal = line.Product.GoodsTypeForFiscal,
            Barcode = line.Product.Barcode
        }, cancellationToken: ct));
    }

    public async Task UpdateLineAsync(long creditGroupId, ReceiptLine line, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dbo.Credit SET
                Price       = @Price,
                PriceOld    = @PriceOld,
                Quantity    = @Quantity,
                Summa       = @Summa,
                DiscountSum = @DiscountSum
            WHERE CreditGroupId = @CreditGroupId
              AND LineNumber     = @LineNumber;
            """;

        await using var conn = new SqlConnection(_connectionString);
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            CreditGroupId = creditGroupId,
            LineNumber = line.LineNumber,
            Price = line.Price,
            PriceOld = line.PriceOld,
            Quantity = line.Quantity,
            Summa = line.TotalSum,
            DiscountSum = line.DiscountSum
        }, cancellationToken: ct));
    }

    public async Task UpdateDiscountCardAsync(
        long creditGroupId, long discountCardId, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dbo.CreditGroup SET
                DiscountCardId = @DiscountCardId
            WHERE CreditGroupId = @CreditGroupId;
            """;

        await using var conn = new SqlConnection(_connectionString);
        await conn.ExecuteAsync(new CommandDefinition(sql,
            new { CreditGroupId = creditGroupId, DiscountCardId = discountCardId },
            cancellationToken: ct));
    }

    public async Task UpdateFiscalInfoAsync(
        long creditGroupId, string fiscalRegNumber, int docNumber, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dbo.CreditGroup SET
                FiscalRegNumber    = @FiscalRegNumber,
                ReceiptNumber      = @DocNumber,
                CreditGroupTimeEnd = @Now
            WHERE CreditGroupId = @CreditGroupId;
            """;

        await using var conn = new SqlConnection(_connectionString);
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            CreditGroupId = creditGroupId,
            FiscalRegNumber = fiscalRegNumber,
            DocNumber = docNumber,
            Now = DateTime.Now
        }, cancellationToken: ct));
    }
}