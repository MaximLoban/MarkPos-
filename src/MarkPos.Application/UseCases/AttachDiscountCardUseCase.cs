using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MarkPos.Application.Interfaces;
using MarkPos.Domain.Entities;
using MarkPos.Domain.ValueObjects;

namespace MarkPos.Application.UseCases;

public class AttachDiscountCardUseCase
{
    private readonly IDiscountCardRepository _cards;
    private readonly RequestDiscountsUseCase _requestDiscounts;

    public AttachDiscountCardUseCase(
        IDiscountCardRepository cards,
        RequestDiscountsUseCase requestDiscounts)
    {
        _cards = cards;
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

        // Сразу отправляем запрос в дисконтный модуль с картой
        await _requestDiscounts.ExecuteAsync(receipt, ct);

        return Result<DiscountCard>.Ok(card);
    }
}