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

        var paymentsSum = payments.Where(p => p.Sum > 0).Sum(p => p.Sum);
        if (paymentsSum < receipt.TotalSum)
            return Result<TitanCheckResult>.Failure(
                $"Сумма оплат {paymentsSum:F2} меньше суммы чека {receipt.TotalSum:F2}");

        var checkRequest = BuildRequest(receipt, payments);

        // Первая попытка
        var titanResult = await _titanPos.RegisterCheckAsync(checkRequest, ct);

        // Ошибка 11 = смена открыта более 24ч — нужно сначала закрыть открытые документы
        if (!titanResult.IsSuccess && titanResult.Error!.Contains("11"))
        {
            // Получаем текущее состояние кассы
            var infoResult = await _titanPos.GetInfoAsync(ct);
            if (!infoResult.IsSuccess)
                return Result<TitanCheckResult>.Failure(
                    $"Не удалось получить статус кассы: {infoResult.Error}");

            var info = infoResult.Value!;

            // Аннулируем последний чек если нужно
            if (info.DocNumber > 0)
            {
                var rollback = await _titanPos.RollbackAsync(info.DocNumber, ct);
                // Игнорируем ошибку rollback — продолжаем
            }

            // Изымаем наличные если они есть
            if (info.CashInAll > 0)
            {
                var moneyOut = await _titanPos.MoneyOrderAsync(info.CashInAll, isDeposit: false, ct);
                if (!moneyOut.IsSuccess)
                    return Result<TitanCheckResult>.Failure(
                        $"Не удалось изъять наличные: {moneyOut.Error}");
            }

            // Закрываем смену (Z-отчёт)
            var closeShift = await _titanPos.CloseShiftAsync(ct);
            if (!closeShift.IsSuccess)
                return Result<TitanCheckResult>.Failure(
                    $"Не удалось закрыть смену: {closeShift.Error}");

            // Открываем новую смену
            var openShift = await _titanPos.OpenShiftAsync(ct);
            if (!openShift.IsSuccess)
                return Result<TitanCheckResult>.Failure(
                    $"Не удалось открыть новую смену: {openShift.Error}");

            // Повторяем оплату
            titanResult = await _titanPos.RegisterCheckAsync(checkRequest, ct);
        }

        if (!titanResult.IsSuccess)
            return Result<TitanCheckResult>.Failure(titanResult.Error!);

        var fiscalData = titanResult.Value!;
        receipt.MarkAsClosed(fiscalData.Uid, fiscalData.DocNumber);

        if (receipt.CreditGroupId.HasValue)
        {
            await _receipts.UpdateFiscalInfoAsync(
                receipt.CreditGroupId.Value,
                fiscalData.Uid,
                fiscalData.DocNumber,
                ct);
        }

        return Result<TitanCheckResult>.Ok(fiscalData);
    }

    private static TitanCheckRequest BuildRequest(
        Receipt receipt, IReadOnlyList<TitanPayment> payments) =>
        new(
            Lines: receipt.Lines.Select(l => new TitanCheckLine(
                GoodsName: l.Product.Name,
                Barcode: l.Product.Gtin ?? l.Product.Barcode,
                CodeType: l.Product.TitanCodeType,
                Price: l.PriceOld,
                Quantity: l.Quantity,
                DiscountSum: l.DiscountSum,
                TotalSum: l.TotalSum
            )).ToList(),
            Payments: payments,
            TotalSum: receipt.TotalSum,
            DiscountSum: receipt.DiscountSum
        );
}