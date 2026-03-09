using MarkPos.Application.Interfaces;
using MarkPos.Domain.Entities;

namespace MarkPos.Infrastructure.Persistence;

/// <summary>
/// Временная заглушка для MVP. Полная реализация — следующий этап.
/// </summary>
public class ReceiptRepository : IReceiptRepository
{
    public Task SaveAsync(Receipt receipt, CancellationToken ct = default)
    {
        // TODO: сохранить в CreditGroup + Credit[]
        return Task.CompletedTask;
    }

    public Task UpdateFiscalInfoAsync(int receiptId, string fiscalRegNumber, int docNumber, CancellationToken ct = default)
    {
        // TODO: обновить FiscalRegNumber в CreditGroup
        return Task.CompletedTask;
    }
}