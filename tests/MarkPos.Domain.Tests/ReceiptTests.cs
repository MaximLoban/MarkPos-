using MarkPos.Domain.Entities;
using MarkPos.Domain.ValueObjects;

namespace MarkPos.Domain.Tests;

public class ReceiptTests
{
    private static Product MakeProduct(long id = 1, decimal price = 10.00m) => new()
    {
        GoodsId = id,
        GoodsGroupId = 1,
        Name = $"Товар {id}",
        Price = price,
        Piece = 1
    };

    [Fact]
    public void AddItem_NewProduct_AddsLine()
    {
        var receipt = Receipt.New(1);
        var result = receipt.AddItem(MakeProduct(), 1);

        Assert.True(result.IsSuccess);
        Assert.Single(receipt.Lines);
        Assert.Equal(1, receipt.Lines[0].LineNumber);
    }

    [Fact]
    public void AddItem_SameProduct_IncreasesQuantity()
    {
        var receipt = Receipt.New(1);
        var product = MakeProduct();

        receipt.AddItem(product, 1);
        receipt.AddItem(product, 2);

        Assert.Single(receipt.Lines);
        Assert.Equal(3, receipt.Lines[0].Quantity);
    }

    [Fact]
    public void AddItem_ZeroQuantity_Fails()
    {
        var receipt = Receipt.New(1);
        var result = receipt.AddItem(MakeProduct(), 0);

        Assert.False(result.IsSuccess);
        Assert.Empty(receipt.Lines);
    }

    [Fact]
    public void RemoveItem_Existing_RemovesLine()
    {
        var receipt = Receipt.New(1);
        receipt.AddItem(MakeProduct(1), 1);
        receipt.AddItem(MakeProduct(2), 1);

        var result = receipt.RemoveItem(1);

        Assert.True(result.IsSuccess);
        Assert.Single(receipt.Lines);
        Assert.Equal(2, receipt.Lines[0].LineNumber);
    }

    [Fact]
    public void RemoveItem_NotExisting_Fails()
    {
        var receipt = Receipt.New(1);
        var result = receipt.RemoveItem(99);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void TotalSum_CalculatesCorrectly()
    {
        var receipt = Receipt.New(1);
        receipt.AddItem(MakeProduct(1, price: 5.00m), 2);
        receipt.AddItem(MakeProduct(2, price: 3.50m), 1);

        Assert.Equal(13.50m, receipt.TotalSum);
    }

    [Fact]
    public void ApplyDiscounts_UpdatesPriceAndStatus()
    {
        var receipt = Receipt.New(1);
        receipt.AddItem(MakeProduct(1, price: 10.00m), 1);

        var discounts = new List<DiscountLineResult>
        {
            new(LineNumber: 1, Price: 10.00m, DiscountSum: 1.00m, SumAdd: 0, DiscountOutDescription: "-10%")
        };

        var result = receipt.ApplyDiscounts(discounts);

        Assert.True(result.IsSuccess);
        Assert.Equal(ReceiptStatus.DiscountsApplied, receipt.Status);
        Assert.Equal(9.00m, receipt.TotalSum);
    }

    [Fact]
    public void MarkAsClosed_SetsStatusAndFiscalInfo()
    {
        var receipt = Receipt.New(1);
        receipt.AddItem(MakeProduct(), 1);

        var result = receipt.MarkAsClosed("REG-001", 42);

        Assert.True(result.IsSuccess);
        Assert.Equal(ReceiptStatus.Closed, receipt.Status);
        Assert.Equal("REG-001", receipt.FiscalRegNumber);
        Assert.Equal(42, receipt.FiscalDocNumber);
    }

    [Fact]
    public void AddItem_ToClosedReceipt_Fails()
    {
        var receipt = Receipt.New(1);
        receipt.AddItem(MakeProduct(), 1);
        receipt.MarkAsClosed("REG-001", 1);

        var result = receipt.AddItem(MakeProduct(2), 1);

        Assert.False(result.IsSuccess);
    }
}
