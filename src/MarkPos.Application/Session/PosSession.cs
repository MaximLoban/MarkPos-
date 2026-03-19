using MarkPos.Application.DTOs;
using MarkPos.Application.Sales;
using MarkPos.Application.UseCases;
using MarkPos.Domain.Entities;
using MarkPos.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace MarkPos.Application.Session;

public sealed class PosSession : IPosSession
{
    private readonly AddItemByBarcodeUseCase _addItem;
    private readonly RemoveItemUseCase _removeItem;
    private readonly RequestDiscountsUseCase _requestDiscounts;
    private readonly CloseReceiptUseCase _closeReceipt;
    private readonly AttachDiscountCardUseCase _attachCard;
    private readonly ILogger<PosSession> _logger;
    private readonly StationConfig _config;

    private Receipt _receipt;
    private CancellationTokenSource? _discountCts;

    public PosState State { get; private set; } = PosState.Empty;
    public event Action<PosState>? StateChanged;

    public PosSession(
     AddItemByBarcodeUseCase addItem,
     RemoveItemUseCase removeItem,
     RequestDiscountsUseCase requestDiscounts,
     CloseReceiptUseCase closeReceipt,
     AttachDiscountCardUseCase attachCard,
     StationConfig config,
     ILogger<PosSession> logger)
    {
        _addItem = addItem;
        _removeItem = removeItem;
        _requestDiscounts = requestDiscounts;
        _closeReceipt = closeReceipt;
        _attachCard = attachCard;
        _config = config;
        _logger = logger;

        _receipt = NewReceipt();
    }

    private Receipt NewReceipt()
    {
        var receipt = Receipt.New(1);
        receipt.Configure(
            int.Parse(_config.StationNumber),
            int.Parse(_config.FiscalType),
            short.Parse(_config.StationSaleTypeId));
        return receipt;
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    public async Task ScanItemAsync(string barcode, decimal quantity = 1, CancellationToken ct = default)
    {
        _logger.LogInformation("ScanItem: {Barcode} qty={Qty}", barcode, quantity);

        var result = await _addItem.ExecuteAsync(_receipt, barcode, quantity, ct);
        if (!result.IsSuccess)
        {
            Publish(result.Error, PosMessageType.Error);
            return;
        }

        Publish();
        await RequestDiscountsAsync();
    }

    public async Task RemoveItemAsync(int lineNumber)
    {
        _logger.LogInformation("RemoveItem: line={Line}", lineNumber);

        var result = _removeItem.Execute(_receipt, lineNumber);
        if (!result.IsSuccess)
        {
            Publish(result.Error, PosMessageType.Warning);
            return;
        }

        Publish();
        await RequestDiscountsAsync();
    }

    public async Task AdjustQuantityAsync(int lineNumber, decimal newQuantity)
    {
        var result = _receipt.SetQuantity(lineNumber, newQuantity);
        if (!result.IsSuccess)
        {
            Publish(result.Error, PosMessageType.Warning);
            return;
        }

        Publish();
        await RequestDiscountsAsync();
    }

    public async Task AttachCardAsync(string cardNumber)
    {
        _logger.LogInformation("AttachCard: {Card}", cardNumber);

        var result = await _attachCard.ExecuteAsync(_receipt, cardNumber);
        if (!result.IsSuccess)
        {
            Publish(result.Error, PosMessageType.Warning);
            return;
        }

        Publish($"Карта принята: {cardNumber}", PosMessageType.Info);
        await RequestDiscountsAsync();
    }

    public async Task<Result<TitanCheckResult>> PayAsync()
    {
        _logger.LogInformation("Pay: total={Total}", _receipt.TotalSum);

        if (!_receipt.Lines.Any())
        {
            Publish("Нет товаров в чеке", PosMessageType.Warning);
            return Result<TitanCheckResult>.Failure("Нет товаров в чеке");
        }

        var payments = new List<TitanPayment>
        {
            new(Sum: _receipt.TotalSum, TypeFlag: 1)
        };

        var result = await _closeReceipt.ExecuteAsync(_receipt, payments);
        if (!result.IsSuccess)
        {
            Publish(result.Error, PosMessageType.Error);
            return result;
        }

        _logger.LogInformation("Receipt closed: doc={DocNumber}", result.Value!.DocNumber);
        _receipt = NewReceipt();
        Publish();
        return result;
    }

    public void Cancel()
    {
        _logger.LogInformation("Receipt cancelled");
        _discountCts?.Cancel();
        _receipt = NewReceipt();
        Publish();
    }

    // ── Private ────────────────────────────────────────────────────────────────

    private async Task RequestDiscountsAsync()
    {
        _discountCts?.Cancel();
        _discountCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(200, _discountCts.Token);
            _logger.LogDebug("Requesting discounts, lines={Count}", _receipt.Lines.Count);
            await _requestDiscounts.ExecuteAsync(_receipt, _discountCts.Token);
            Publish();
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("RequestDiscounts cancelled (debounce)");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Discount module error");
            Publish($"Скидки недоступны: {ex.Message}", PosMessageType.Warning);
        }
    }

    private void Publish(string? message = null, PosMessageType type = PosMessageType.None)
    {
        State = new PosState(
            Lines: _receipt.Lines,
            TotalSum: _receipt.TotalSum,
            DiscountSum: _receipt.DiscountSum,
            Status: _receipt.Status,
            Message: message,
            MessageType: type,
            DiscountCardNumber: _receipt.DiscountCard?.CardNumber
        );
        StateChanged?.Invoke(State);
    }
}