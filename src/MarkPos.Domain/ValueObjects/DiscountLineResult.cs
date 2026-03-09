using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarkPos.Domain.ValueObjects;

/// <summary>
/// Результат по одной строке от дисконтного модуля.
/// Маппится из Credit[] в ответе дисконтного сервиса.
/// </summary>
public record DiscountLineResult(
    int LineNumber,
    decimal Price,
    decimal DiscountSum,
    decimal SumAdd,
    string DiscountOutDescription
);
