using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MarkPos.Application.DTOs;
using MarkPos.Domain.ValueObjects;

namespace MarkPos.Application.Interfaces;

public interface IDiscountClient
{
    Task<Result<DiscountResponse>> RequestDiscountsAsync(
        DiscountRequest request,
        CancellationToken ct = default);
}
