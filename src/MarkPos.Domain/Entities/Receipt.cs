using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MarkPos.Domain.ValueObjects;

namespace MarkPos.Domain.Entities;

/// <summary>
/// Агрегат чека. Вся бизнес-логика формирования чека здесь.
/// Соответствует CreditGroup + Credit[] в БД.
/// </summary>
public class Receipt
{
    private readonly List<ReceiptLine> _lines = new();

    public int Id { get; }                  // TokenId для дисконтного модуля
    public ReceiptStatus Status { get; private set; }
    public IReadOnlyList<ReceiptLine> Lines => _lines.AsReadOnly();
    public decimal TotalSum => _lines.Sum(l => l.TotalSum);
    public decimal DiscountSum => _lines.Sum(l => l.DiscountSum);
    public DateTime CreatedAt { get; }

    // Заполняется после фискализации через TitanPOS
    public string? FiscalRegNumber { get; private set; }
    public int? FiscalDocNumber { get; private set; }
    public DateTime? ClosedAt { get; private set; }

    private Receipt(int id)
    {
        Id = id;
        Status = ReceiptStatus.Draft;
        CreatedAt = DateTime.Now;
    }

    public static Receipt New(int id) => new(id);

    public Result AddItem(Product product, decimal quantity)
    {
        if (Status == ReceiptStatus.Closed)
            return Result.Failure("Нельзя изменять закрытый чек");

        if (quantity <= 0)
            return Result.Failure("Количество должно быть больше нуля");

        var existing = _lines.FirstOrDefault(l => l.Product.GoodsId == product.GoodsId);
        if (existing != null)
            existing.AddQuantity(quantity);
        else
        {
            var lineNumber = _lines.Count == 0 ? 1 : _lines.Max(l => l.LineNumber) + 1;
            _lines.Add(new ReceiptLine(lineNumber, product, quantity));
        }

        Status = ReceiptStatus.Draft;
        return Result.Ok();
    }

    public Result RemoveItem(int lineNumber)
    {
        if (Status == ReceiptStatus.Closed)
            return Result.Failure("Нельзя изменять закрытый чек");

        var line = _lines.FirstOrDefault(l => l.LineNumber == lineNumber);
        if (line == null)
            return Result.Failure($"Позиция {lineNumber} не найдена");

        _lines.Remove(line);
        Status = ReceiptStatus.Draft;
        return Result.Ok();
    }

    public Result ApplyDiscounts(IReadOnlyList<DiscountLineResult> discounts)
    {
        if (!_lines.Any())
            return Result.Failure("Нет позиций для применения скидок");

        foreach (var discount in discounts)
        {
            var line = _lines.FirstOrDefault(l => l.LineNumber == discount.LineNumber);
            if (line == null) continue;
            line.ApplyDiscount(discount.Price, discount.DiscountSum, discount.SumAdd);
        }

        Status = ReceiptStatus.DiscountsApplied;
        return Result.Ok();
    }

    public Result MarkAsClosed(string fiscalRegNumber, int fiscalDocNumber)
    {
        if (Status == ReceiptStatus.Closed)
            return Result.Failure("Чек уже закрыт");

        FiscalRegNumber = fiscalRegNumber;
        FiscalDocNumber = fiscalDocNumber;
        ClosedAt = DateTime.Now;
        Status = ReceiptStatus.Closed;
        return Result.Ok();
    }
}

public enum ReceiptStatus
{
    Draft,            // Формируется
    DiscountsApplied, // Скидки применены
    Closed            // Фискализирован
}