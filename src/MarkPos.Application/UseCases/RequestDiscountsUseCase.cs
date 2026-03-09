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

public class RequestDiscountsUseCase
{
    private readonly IDiscountClient _discount;
    private readonly StationConfig _config;

    public RequestDiscountsUseCase(IDiscountClient discount, StationConfig config)
    {
        _discount = discount;
        _config = config;
    }

    public async Task<Result> ExecuteAsync(Receipt receipt, CancellationToken ct = default)
    {
        if (!receipt.Lines.Any())
            return Result.Ok();

        var request = BuildRequest(receipt);

        var response = await _discount.RequestDiscountsAsync(request, ct);
        if (!response.IsSuccess)
            return Result.Failure(response.Error!);

        var discountResults = response.Value!.Credit
            .Select(c => new DiscountLineResult(
                LineNumber: c.LineNumber,
                Price: c.Price,
                DiscountSum: c.DiscountOut,
                SumAdd: c.SumAdd,
                DiscountOutDescription: c.DiscountOutDescription))
            .ToList();

        return receipt.ApplyDiscounts(discountResults);
    }

    private DiscountRequest BuildRequest(Receipt receipt)
    {
        var creditGroup = new DiscountCreditGroup(
            BaseId: "1",
            ShopNumber: _config.ShopNumber,
            StationNumber: _config.StationNumber,
            FiscalType: _config.FiscalType,
            CashboxType: _config.CashboxType,
            CreditGroupId: "0",
            IsReturn: "0",
            IsLast: "1",
            CreditGroupGUID: Guid.NewGuid().ToString().ToUpper(),
            StationSaleTypeId: _config.StationSaleTypeId
        );

        var creditItems = receipt.Lines.Select(l => new DiscountCreditItem(
            LineNumber: l.LineNumber.ToString(),
            GoodsId: l.Product.GoodsId.ToString(),
            DiscountGroupId: l.Product.DiscountGroupId?.ToString() ?? "1",
            GoodsGroupId: l.Product.GoodsGroupId.ToString(),
            Quantity: l.Quantity.ToString("F5"),
            Price: l.Price.ToString("F2"),
            PriceOld: l.PriceOld.ToString("F2")
        )).ToList();

        return new DiscountRequest(
            TokenId: receipt.Id.ToString(),
            ShopNumber: _config.ShopNumber,
            StationNumber: _config.StationNumber,
            CreditGroup: new[] { creditGroup },
            Credit: creditItems
        );
    }
}