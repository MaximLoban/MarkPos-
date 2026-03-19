using MarkPos.Application.Interfaces;
using MarkPos.Domain.Entities;
using MarkPos.Domain.ValueObjects;

namespace MarkPos.Application.UseCases;

public class AttachDiscountCardUseCase
{
    private readonly IDiscountCardRepository _cards;
    private readonly IReceiptRepository _receipts;
    private readonly RequestDiscountsUseCase _requestDiscounts;

    public AttachDiscountCardUseCase(
        IDiscountCardRepository cards,
        IReceiptRepository receipts,
        RequestDiscountsUseCase requestDiscounts)
    {
        _cards = cards;
        _receipts = receipts;
        _requestDiscounts = requestDiscounts;
    }

    public async Task<Result<DiscountCard>> ExecuteAsync(
        Receipt receipt,
        string cardNumber,
        CancellationToken ct = default)
    {
        var card = await _cards.FindByNumberAsync(cardNumber, ct);
        if (card == null)
            return Result<DiscountCard>.Failure($"Дисконтная карта {cardNumber} не найдена");

        var attachResult = receipt.AttachDiscountCard(card);
        if (!attachResult.IsSuccess)
            return Result<DiscountCard>.Failure(attachResult.Error!);

        // UPDATE CreditGroup.DiscountCardId если чек уже создан в БД
        if (receipt.CreditGroupId.HasValue)
        {
            await _receipts.UpdateDiscountCardAsync(
                receipt.CreditGroupId.Value,
                card.DiscountCardId,
                ct);
        }

        // Пересчёт скидок с картой
        await _requestDiscounts.ExecuteAsync(receipt, ct);

        return Result<DiscountCard>.Ok(card);
    }
}