using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarkPos.Domain.Entities;

/// <summary>
/// Строка чека. Соответствует записи в таблице Credit.
/// LineNumber стабилен — используется как ключ для дисконтного модуля.
/// </summary>
public class ReceiptLine
{
    public int LineNumber { get; }
    public Product Product { get; }
    public decimal Quantity { get; private set; }
    public decimal Price { get; private set; }
    public decimal PriceOld { get; }
    public decimal DiscountSum { get; private set; }
    public decimal SumAdd { get; private set; }

    public decimal TotalSum => Math.Round(Price * Quantity + SumAdd, 5);

    internal ReceiptLine(int lineNumber, Product product, decimal quantity)
    {
        LineNumber = lineNumber;
        Product = product;
        Quantity = quantity;
        Price = product.Price;
        PriceOld = product.Price;
    }

    internal void AddQuantity(decimal delta)
    {
        Quantity += delta;
    }

    internal void ApplyDiscount(decimal newPrice, decimal discountSum, decimal sumAdd)
    {
        Price = newPrice;
        DiscountSum = discountSum;
        SumAdd = sumAdd;
    }

    internal void SetQuantity(decimal quantity)
    {
        Quantity = quantity;
    }
}