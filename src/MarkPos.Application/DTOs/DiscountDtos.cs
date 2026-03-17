using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarkPos.Application.DTOs;

/// <summary>
/// Запрос к дисконтному модулю.
/// Структура взята из реальных логов протокола.
/// </summary>
public record DiscountRequest(
    string TokenId,
    string ShopNumber,
    string StationNumber,
    string DiscountCardId,
    string DiscountCardGroupId,
    string DiscountCardGroupId2,
    IReadOnlyList<DiscountCreditGroup> CreditGroup,
    IReadOnlyList<DiscountCreditItem> Credit
);
public record DiscountCreditGroup(
    string BaseId,
    string ShopNumber,
    string StationNumber,
    string FiscalType,
    string CashboxType,
    string CreditGroupId,
    string WhatBase,
    string DiscountCardId,
    string DiscountCardTypeId,
    string DiscountCardGroupId,
    string DiscountCardGroupId2,
    string DiscountCardNumber,
    string IsReturn,
    string IsLast,
    string Checkx,
    string OrderGroupTime,
    string CreditGroupGUID,
    string StationSaleTypeId
);

public record DiscountCreditItem(
    string LineNumber,
    string GoodsId,
    string GoodsGroupId,
    string DiscountGroupId,
    string GoodsTypeId10,
    string Discount,
    string Quantity,
    string GoodsMinQuantity,
    string CreditIsReturn,
    string Price,
    string PriceOld,
    string DiscountPercent,
    string DiscountSum,
    string PriceSpecial01,
    string PriceSpecial02,
    string PriceSpecial03,
    string PriceSpecial04,
    string PriceSpecial05,
    string PriceSpecial06,
    string PriceSpecial07,
    string PriceSpecial08,
    string PriceSpecial09,
    string GoodsType,
    string CheckSN
);

public record DiscountResponse(
    string TokenId,
    IReadOnlyList<DiscountResponseLine> Credit
);

public record DiscountResponseLine(
    string LineNumber,
    string GoodsId,
    string Price,
    string PriceOld,
    string Quantity,
    string SumAdd,
    string CoinsAdd,
    string DiscountOut,
    string DiscountOutDescription
);