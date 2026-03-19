using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MarkPos.Domain.Entities;

namespace MarkPos.Application.DTOs;

/// <summary>
/// Запрос на регистрацию чека в TitanPOS (Operation: check).
/// </summary>
public record TitanCheckRequest(
    IReadOnlyList<TitanCheckLine> Lines,
    IReadOnlyList<TitanPayment> Payments,
    decimal TotalSum,
    decimal DiscountSum,
    bool Refund = false
);

public record TitanCheckLine(
    string GoodsName,
    string? Barcode,
    int CodeType,
    decimal Price,
    decimal Quantity,
    decimal DiscountSum,
    decimal TotalSum
);

public record TitanPayment(
    decimal Sum,
    int TypeFlag,       // 1=наличные 2=карта 3=прочие
    string? Description = null
);

/// <summary>
/// Ответ TitanPOS после успешной фискализации.
/// </summary>
public record TitanCheckResult(
    int DocNumber,
    string Position,
    string Uid,
    string? ElCheckUrl
);

public record TitanPosInfo(
    bool ShiftOpened,
    string Serial,
    string Version,
    bool IsBlocked,
    string? BlockedText,
    int DocNumber,    // ← НОВОЕ
    decimal CashInAll    // ← НОВОЕ
);
public record TitanMoneyOrderResult(
    int Number,
    string Position,
    string Uid
);