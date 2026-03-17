using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MarkPos.Application.DTOs;
using MarkPos.Domain.ValueObjects;

namespace MarkPos.Application.Session;

public interface IPosSession
{
    PosState State { get; }
    event Action<PosState> StateChanged;

    Task ScanItemAsync(string barcode, decimal quantity = 1, CancellationToken ct = default);
    Task RemoveItemAsync(int lineNumber);
    Task AttachCardAsync(string cardNumber);
    Task<Result<TitanCheckResult>> PayAsync();   // ← было ClosedReceiptDto
    void Cancel();
}