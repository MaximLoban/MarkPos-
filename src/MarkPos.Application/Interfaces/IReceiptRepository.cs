using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MarkPos.Domain.Entities;

namespace MarkPos.Application.Interfaces;

public interface IReceiptRepository
{
    /// <summary>Сохранить чек в локальную MSSQL (CreditGroup + Credit[]).</summary>
    Task SaveAsync(Receipt receipt, CancellationToken ct = default);

    /// <summary>Обновить фискальные данные после закрытия через TitanPOS.</summary>
    Task UpdateFiscalInfoAsync(int receiptId, string fiscalRegNumber, int docNumber, CancellationToken ct = default);
}
