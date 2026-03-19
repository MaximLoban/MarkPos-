using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MarkPos.Application.DTOs;
using MarkPos.Application.Interfaces;
using MarkPos.Domain.Entities;
using MarkPos.Domain.ValueObjects;
using System.Globalization;

namespace MarkPos.Application.UseCases;

public class RequestDiscountsUseCase
{
    private readonly IDiscountClient _discount;
    private readonly StationConfig _config;
    private readonly IReceiptRepository _receipts;

    public RequestDiscountsUseCase(IDiscountClient discount, StationConfig config, IReceiptRepository receipts)
    {
        _discount = discount;
        _config = config;
        _receipts = receipts;
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
     .Select(c =>
     {
         var newPrice = decimal.Parse(c.Price, System.Globalization.CultureInfo.InvariantCulture);
         var sumAdd = decimal.Parse(c.SumAdd, System.Globalization.CultureInfo.InvariantCulture);

         // Находим строку чека чтобы взять количество
         var line = receipt.Lines.FirstOrDefault(l => l.LineNumber == int.Parse(c.LineNumber));
         var quantity = line?.Quantity ?? 1;
         var priceOld = line?.PriceOld ?? newPrice;

         // Скидка = (старая цена - новая цена) * количество
         var discountSum = (priceOld - newPrice) * quantity;

         return new DiscountLineResult(
             LineNumber: int.Parse(c.LineNumber),
             Price: newPrice,
             DiscountSum: discountSum < 0 ? 0 : discountSum,
             SumAdd: sumAdd,
             DiscountOutDescription: c.DiscountOutDescription);
     })
     .ToList();

        var applyResult = receipt.ApplyDiscounts(discountResults);
        if (!applyResult.IsSuccess)
            return applyResult;

        // UPDATE Credit с новыми ценами и скидками
        if (receipt.CreditGroupId.HasValue)
        {
            foreach (var line in receipt.Lines)
                await _receipts.UpdateLineAsync(receipt.CreditGroupId.Value, line, ct);
        }

        return applyResult;
    }

    private DiscountRequest BuildRequest(Receipt receipt)
    {
        var card = receipt.DiscountCard;

        var creditGroup = new DiscountCreditGroup(
            BaseId: _config.BaseId,
            ShopNumber: _config.ShopNumber,
            StationNumber: _config.StationNumber,
            FiscalType: _config.FiscalType,
            CashboxType: _config.CashboxType,
            CreditGroupId: "0",
            WhatBase: "11",
            DiscountCardId: card?.DiscountCardId.ToString() ?? "0",
            DiscountCardTypeId: card?.DiscountCardTypeId?.ToString() ?? "0",
            DiscountCardGroupId: card?.DiscountCardGroupId.ToString() ?? "0",
            DiscountCardGroupId2: card?.DiscountCardGroupId2?.ToString() ?? "0",
            DiscountCardNumber: card?.CardNumber ?? "",
            IsReturn: "0",
            IsLast: "0",
            Checkx: "0",
            OrderGroupTime: "",
            CreditGroupGUID: Guid.NewGuid().ToString().ToUpper(),
            StationSaleTypeId: _config.StationSaleTypeId
        );

        var creditItems = receipt.Lines.Select(l => new DiscountCreditItem(
            LineNumber: l.LineNumber.ToString(),
            GoodsId: l.Product.GoodsId.ToString(),
            GoodsGroupId: l.Product.GoodsGroupId.ToString(),
            DiscountGroupId: l.Product.DiscountGroupId?.ToString() ?? "1",
            GoodsTypeId10: "0",
            Discount: "0",
            Quantity: l.Quantity.ToString("F5", CultureInfo.InvariantCulture),
            GoodsMinQuantity: l.Product.GoodsMinQuantity?.ToString("F3", CultureInfo.InvariantCulture) ?? "0",
            CreditIsReturn: "0",
            Price: l.Price.ToString("F2", CultureInfo.InvariantCulture),
            PriceOld: l.PriceOld.ToString("F2", CultureInfo.InvariantCulture),
            DiscountPercent: "0",
            DiscountSum: l.DiscountSum.ToString("F5", CultureInfo.InvariantCulture),
            PriceSpecial01: "0",
            PriceSpecial02: "0",
            PriceSpecial03: "0",
            PriceSpecial04: "0",
            PriceSpecial05: "0",
            PriceSpecial06: "0",
            PriceSpecial07: "0",
            PriceSpecial08: "0",
            PriceSpecial09: "0",
            GoodsType: "0",
            CheckSN: "0"
        )).ToList();

        return new DiscountRequest(
            TokenId: receipt.Id.ToString(),
            ShopNumber: _config.ShopNumber,
            StationNumber: _config.StationNumber,
            DiscountCardId: card?.DiscountCardId.ToString() ?? "0",
            DiscountCardGroupId: card?.DiscountCardGroupId.ToString() ?? "0",
            DiscountCardGroupId2: card?.DiscountCardGroupId2?.ToString() ?? "0",
            CreditGroup: new[] { creditGroup },
            Credit: creditItems
        );
    }
}