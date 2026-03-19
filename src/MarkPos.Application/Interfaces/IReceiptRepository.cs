using MarkPos.Domain.Entities;

namespace MarkPos.Application.Interfaces;

public interface IReceiptRepository
{
    /// <summary>INSERT CreditGroup, возвращает CreditGroupId</summary>
    Task<long> CreateGroupAsync(Receipt receipt, CancellationToken ct = default);

    /// <summary>INSERT одну строку Credit</summary>
    Task InsertLineAsync(long creditGroupId, ReceiptLine line, CancellationToken ct = default);

    /// <summary>UPDATE строки Credit после пересчёта скидок</summary>
    Task UpdateLineAsync(long creditGroupId, ReceiptLine line, CancellationToken ct = default);

    /// <summary>UPDATE CreditGroup.DiscountCardId после сканирования карты</summary>
    Task UpdateDiscountCardAsync(long creditGroupId, long discountCardId, CancellationToken ct = default);

    /// <summary>UPDATE CreditGroup после фискализации</summary>
    Task UpdateFiscalInfoAsync(long creditGroupId, string fiscalRegNumber, int docNumber, CancellationToken ct = default);
}