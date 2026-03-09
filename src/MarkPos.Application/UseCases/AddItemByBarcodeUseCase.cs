using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MarkPos.Application.Interfaces;
using MarkPos.Domain.Entities;
using MarkPos.Domain.ValueObjects;

namespace MarkPos.Application.UseCases;

public class AddItemByBarcodeUseCase
{
    private readonly IProductRepository _products;

    public AddItemByBarcodeUseCase(IProductRepository products)
        => _products = products;

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

        var addResult = receipt.AddItem(product, quantity);
        if (!addResult.IsSuccess)
            return Result<Product>.Failure(addResult.Error!);

        return Result<Product>.Ok(product);
    }
}