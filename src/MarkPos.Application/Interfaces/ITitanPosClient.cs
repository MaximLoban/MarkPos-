using MarkPos.Application.DTOs;
using MarkPos.Domain.ValueObjects;

namespace MarkPos.Application.Interfaces;

public interface ITitanPosClient
{
    Task<Result> InitAsync(CancellationToken ct = default);
    Task<Result> OpenSessionAsync(string pin, string cashierName, CancellationToken ct = default);
    Task<Result> OpenShiftAsync(CancellationToken ct = default);       // ← НОВОЕ
    Task<Result> CloseShiftAsync(CancellationToken ct = default);      // ← НОВОЕ
    Task<Result<TitanCheckResult>> RegisterCheckAsync(TitanCheckRequest request, CancellationToken ct = default);
    Task<Result> CloseSessionAsync(CancellationToken ct = default);
    Task<Result<TitanPosInfo>> GetInfoAsync(CancellationToken ct = default);
}