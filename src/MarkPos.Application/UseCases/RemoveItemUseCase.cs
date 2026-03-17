using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MarkPos.Domain.Entities;
using MarkPos.Domain.ValueObjects;

namespace MarkPos.Application.Sales;

public class RemoveItemUseCase
{
    public Result Execute(Receipt receipt, int lineNumber)
    {
        if (receipt is null)
            return Result.Failure("Чек не инициализирован");

        return receipt.RemoveItem(lineNumber);
    }
}