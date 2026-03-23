using MarkPos.Application.DTOs;
using MarkPos.Application.Interfaces;
using MarkPos.Domain.ValueObjects;

namespace MarkPos.Infrastructure.TitanPos;

/// <summary>
/// Заглушка фискального клиента.
/// Используется когда FiscalEnabled=false или FiscalType=none.
/// Все операции возвращают успех без обращения к железу.
/// </summary>
public sealed class NullFiscalClient : ITitanPosClient
{
    public Task<Result> InitAsync(CancellationToken ct = default)
        => Task.FromResult(Result.Ok());

    public Task<Result> OpenSessionAsync(string pin, string cashierName, CancellationToken ct = default)
        => Task.FromResult(Result.Ok());

    public Task<Result> OpenShiftAsync(CancellationToken ct = default)
        => Task.FromResult(Result.Ok());

    public Task<Result> CloseShiftAsync(CancellationToken ct = default)
        => Task.FromResult(Result.Ok());

    public Task<Result> RollbackAsync(int chequeNumber, CancellationToken ct = default)
        => Task.FromResult(Result.Ok());

    public Task<Result<TitanMoneyOrderResult>> MoneyOrderAsync(decimal totalSum, bool isDeposit, CancellationToken ct = default)
        => Task.FromResult(Result<TitanMoneyOrderResult>.Ok(new TitanMoneyOrderResult(0, "", "")));

    public Task<Result<TitanCheckResult>> RegisterCheckAsync(TitanCheckRequest request, CancellationToken ct = default)
        => Task.FromResult(Result<TitanCheckResult>.Ok(
            new TitanCheckResult(
                DocNumber: 0,
                Position: DateTime.Now.ToString("s"),
                Uid: Guid.NewGuid().ToString("N").ToUpper(),
                ElCheckUrl: null)));

    public Task<Result> CloseSessionAsync(CancellationToken ct = default)
        => Task.FromResult(Result.Ok());

    public Task<Result<TitanPosInfo>> GetInfoAsync(CancellationToken ct = default)
        => Task.FromResult(Result<TitanPosInfo>.Ok(
            new TitanPosInfo(
                ShiftOpened: true,
                Serial: "NULL-CLIENT",
                Version: "0.0.0",
                IsBlocked: false,
                BlockedText: null,
                DocNumber: 0,
                CashInAll: 0m)));
}