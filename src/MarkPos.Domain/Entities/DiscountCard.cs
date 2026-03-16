using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarkPos.Domain.Entities;

/// <summary>
/// Дисконтная карта привязанная к чеку.
/// Данные из MarkPosDiscount.dbo.DiscountCard.
/// </summary>
public class DiscountCard
{
    public long DiscountCardId { get; init; }
    public string CardNumber { get; init; } = string.Empty;
    public long DiscountCardGroupId { get; init; }
    public long? DiscountCardGroupId2 { get; init; }
    public long? DiscountCardTypeId { get; init; }
}