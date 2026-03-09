using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MarkPos.Application.DTOs;
using MarkPos.Application.Interfaces;
using MarkPos.Domain.Entities;
using MarkPos.Domain.ValueObjects;

namespace MarkPos.Application.UseCases;

public class CloseReceiptUseCase
{
    private readonly ITitanPosClient _titanPos;
    private readonly IReceiptRepository _receipts;

    public CloseReceiptUseCase(ITitanPosClient titanPos, IReceiptRepository receipts)
    {
        _titanPos = titanPos;
        _receipts = receipts;
    }

    public async Task<Result<TitanCheckResult>> ExecuteAsync(
        Receipt receipt,
        IReadOnlyList<TitanPayment> payments,
        CancellationToken ct = default)
    {
        if (!receipt.Lines.Any())
            return Result<TitanCheckResult>.Failure("Нельзя закрыть пустой чек");

        if (receipt.Status == ReceiptStatus.Closed)
            return Result<TitanCheckResult>.Failure("Чек уже закрыт");

        // Валидация суммы оплат
        var paymentsSum = payments.Where(p => p.Sum > 0).Sum(p => p.Sum);
        if (paymentsSum < receipt.TotalSum)
            return Result<TitanCheckResult>.Failure(
                $"Сумма оплат {paymentsSum:F2} меньше суммы чека {receipt.TotalSum:F2}");

        // Маппинг в TitanPOS-запрос
        var checkRequest = new TitanCheckRequest(
            Lines: receipt.Lines.Select(l => new TitanCheckLine(
                GoodsName: l.Product.Name,
                Barcode: l.Product.Gtin ?? l.Product.Barcode,
                CodeType: l.Product.TitanCodeType,
                Price: l.Price,
                Quantity: l.Quantity,
                DiscountSum: l.DiscountSum,
                TotalSum: l.TotalSum
            )).ToList(),
            Payments: payments,
            TotalSum: receipt.TotalSum,
            DiscountSum: receipt.DiscountSum
        );

        // Фискализация через TitanPOS
        var titanResult = await _titanPos.RegisterCheckAsync(checkRequest, ct);
        if (!titanResult.IsSuccess)
            return Result<TitanCheckResult>.Failure(titanResult.Error!);

        var fiscalData = titanResult.Value!;

        // Помечаем чек закрытым
        receipt.MarkAsClosed(fiscalData.Uid, fiscalData.DocNumber);

        // Сохраняем в локальную БД
        await _receipts.SaveAsync(receipt, ct);

        return Result<TitanCheckResult>.Ok(fiscalData);
    }
}