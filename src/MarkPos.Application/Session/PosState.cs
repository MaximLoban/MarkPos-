using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarkPos.Domain.Entities;

namespace MarkPos.Application.Session;

public sealed record PosState(
    IReadOnlyList<ReceiptLine> Lines,
    decimal TotalSum,
    decimal DiscountSum,
    ReceiptStatus Status,
    string? Message,
    string? DiscountCardNumber   // ← НОВОЕ
)
{
    public static readonly PosState Empty = new(
        Lines: Array.Empty<ReceiptLine>(),
        TotalSum: 0m,
        DiscountSum: 0m,
        Status: ReceiptStatus.Draft,
        Message: null,
        DiscountCardNumber: null  // ← НОВОЕ
    );

    public bool HasItems => Lines.Count > 0;
    public bool HasDiscount => DiscountSum > 0;
}