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
    string IsReturn,
    string IsLast,
    string CreditGroupGUID,
    string StationSaleTypeId
);

public record DiscountCreditItem(
    string LineNumber,
    string GoodsId,
    string DiscountGroupId,
    string GoodsGroupId,
    string Quantity,
    string Price,
    string PriceOld
);

public record DiscountResponse(
    string TokenId,
    IReadOnlyList<DiscountResponseLine> Credit
);

public record DiscountResponseLine(
    string LineNumber,
    string GoodsId,
    string Price,
    string Quantity,
    string SumAdd,
    string CoinsAdd,
    string DiscountOut,
    string DiscountOutDescription
);