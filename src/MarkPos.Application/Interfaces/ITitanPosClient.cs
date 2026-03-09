using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MarkPos.Application.DTOs;
using MarkPos.Domain.ValueObjects;

namespace MarkPos.Application.Interfaces;

public interface ITitanPosClient
{
    Task<Result> InitAsync(CancellationToken ct = default);
    Task<Result> OpenSessionAsync(string pin, string cashierName, CancellationToken ct = default);
    Task<Result<TitanCheckResult>> RegisterCheckAsync(TitanCheckRequest request, CancellationToken ct = default);
    Task<Result> CloseSessionAsync(CancellationToken ct = default);
    Task<Result<TitanPosInfo>> GetInfoAsync(CancellationToken ct = default);
}
