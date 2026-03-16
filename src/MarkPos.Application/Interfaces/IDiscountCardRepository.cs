using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MarkPos.Domain.Entities;

namespace MarkPos.Application.Interfaces;

public interface IDiscountCardRepository
{
    /// <summary>
    /// Поиск карты по номеру в MarkPosDiscount.dbo.DiscountCard.
    /// </summary>
    Task<DiscountCard?> FindByNumberAsync(string cardNumber, CancellationToken ct = default);
}