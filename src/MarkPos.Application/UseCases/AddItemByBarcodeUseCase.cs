using MarkPos.Application.Interfaces;
using MarkPos.Domain.Entities;
using MarkPos.Domain.ValueObjects;

namespace MarkPos.Application.UseCases;

public class AddItemByBarcodeUseCase
{
    private readonly IProductRepository _products;
    private readonly IReceiptRepository _receipts;

    public AddItemByBarcodeUseCase(IProductRepository products, IReceiptRepository receipts)
    {
        _products = products;
        _receipts = receipts;
    }

    public async Task<Result<Product>> ExecuteAsync(
        Receipt receipt,
        string barcode,
        decimal quantity = 1,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return Result<Product>.Failure("Штрихкод не может быть пустым");

        var product = await _products.FindByBarcodeAsync(barcode, ct);
        if (product == null)
            return Result<Product>.Failure($"Товар со штрихкодом «{barcode}» не найден");

        var isFirstItem = !receipt.Lines.Any();
        // Последний товар до добавления
        var lastLineBeforeAdd = receipt.Lines.Count > 0 ? receipt.Lines[^1] : null;
        var isCurrentItem = lastLineBeforeAdd?.Product.GoodsId == product.GoodsId;

        var addResult = receipt.AddItem(product, quantity);
        if (!addResult.IsSuccess)
            return Result<Product>.Failure(addResult.Error!);

        // Первый товар — создаём CreditGroup в БД
        if (isFirstItem)
        {
            var creditGroupId = await _receipts.CreateGroupAsync(receipt, ct);
            receipt.SetCreditGroupId(creditGroupId);
        }

        if (!receipt.CreditGroupId.HasValue)
            return Result<Product>.Ok(product);

        var line = receipt.Lines[^1]; // всегда берём последнюю строку

        if (isCurrentItem)
        {
            // Тот же товар что и текущий — UPDATE количества
            await _receipts.UpdateLineAsync(receipt.CreditGroupId.Value, line, ct);
        }
        else
        {
            // Новая строка (новый товар или повторный) — INSERT
            await _receipts.InsertLineAsync(receipt.CreditGroupId.Value, line, ct);
        }

        return Result<Product>.Ok(product);
    }
}