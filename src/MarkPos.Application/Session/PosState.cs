using MarkPos.Domain.Entities;

namespace MarkPos.Application.Session;

public enum PosMessageType
{
    None,
    Info,    // "Карта принята" — полоска под хедером
    Warning, // "Скидки недоступны" — полоска под хедером
    Error,    // "Товар не найден" — полноэкранный оверлей
    Success   // Успешно закрытый чек
}

public sealed record PosState(
    IReadOnlyList<ReceiptLine> Lines,
    decimal TotalSum,
    decimal DiscountSum,
    ReceiptStatus Status,
    string? Message,
    PosMessageType MessageType,
    string? DiscountCardNumber,
    string? ElCheckUrl    // ← НОВОЕ
)
{
    public static readonly PosState Empty = new(
        Lines: Array.Empty<ReceiptLine>(),
        TotalSum: 0m,
        DiscountSum: 0m,
        Status: ReceiptStatus.Draft,
        Message: null,
        MessageType: PosMessageType.None,
        DiscountCardNumber: null,
         ElCheckUrl: null    // ← НОВОЕ
    );

    public bool HasItems => Lines.Count > 0;
    public bool HasDiscount => DiscountSum > 0;
}