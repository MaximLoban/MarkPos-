using MarkPos.Application.DTOs;
using MarkPos.Application.Interfaces;
using MarkPos.Domain.ValueObjects;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MarkPos.Infrastructure.TitanPos;

public class TitanPosHttpClient : ITitanPosClient
{
    private readonly HttpClient _http;
    private string _titanKey;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private static readonly object _logLock = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TitanPosHttpClient(HttpClient http, string initialKey)
    {
        _http = http;
        _titanKey = initialKey;
        Log($"создан, initialKey=[{initialKey}]");
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public async Task<Result> InitAsync(CancellationToken ct = default)
    {
        Log("InitAsync вызван");
        var response = await SendAsync<TitanBaseResponse>("init", new { }, ct);
        return response.IsSuccess ? Result.Ok() : Result.Failure(response.Error!);
    }

    public async Task<Result> OpenSessionAsync(string pin, string cashierName, CancellationToken ct = default)
    {
        Log($"OpenSessionAsync: cashier=[{cashierName}]");
        var response = await SendAsync<TitanBaseResponse>("open", new
        {
            Pin = pin,
            Cashier = cashierName
        }, ct);
        return response.IsSuccess ? Result.Ok() : Result.Failure(response.Error!);
    }

    public async Task<Result> OpenShiftAsync(CancellationToken ct = default)
    {
        Log("OpenShiftAsync вызван");
        var response = await SendAsync<TitanBaseResponse>("openShift", new { }, ct);
        return response.IsSuccess ? Result.Ok() : Result.Failure(response.Error!);
    }

    public async Task<Result> CloseShiftAsync(CancellationToken ct = default)
    {
        Log("CloseShiftAsync вызван");
        var response = await SendAsync<TitanBaseResponse>("closeShift", new { }, ct);
        return response.IsSuccess ? Result.Ok() : Result.Failure(response.Error!);
    }

    public async Task<Result<TitanCheckResult>> RegisterCheckAsync(
        TitanCheckRequest request, CancellationToken ct = default)
    {
        var body = new
        {
            Refund = request.Refund,
            Lines = request.Lines.Select(l => new
            {
                Goods = new
                {
                    Name = l.GoodsName,
                    CodeType = l.CodeType,
                    Barcode = l.Barcode
                },
                Price = l.Price,
                Quantity = l.Quantity,
                DiscountSum = l.DiscountSum,
                TotalSum = l.TotalSum
            }),
            DiscountSum = request.DiscountSum,
            TotalSum = request.TotalSum,
            Payments = request.Payments.Select(p => new
            {
                Sum = p.Sum,
                TypeFlag = p.TypeFlag,
                Description = p.Description
            })
        };

        var response = await SendAsync<TitanCheckResponse>("check", body, ct);
        if (!response.IsSuccess)
            return Result<TitanCheckResult>.Failure(response.Error!);

        var data = response.Value!;
        return Result<TitanCheckResult>.Ok(new TitanCheckResult(
            DocNumber: data.Number,
            Position: data.Position,
            Uid: data.UID,
            ElCheckUrl: data.ElCheckUrl
        ));
    }

    public async Task<Result> CloseSessionAsync(CancellationToken ct = default)
    {
        Log("CloseSessionAsync вызван");
        var response = await SendAsync<TitanBaseResponse>("close", new { }, ct);
        return response.IsSuccess ? Result.Ok() : Result.Failure(response.Error!);
    }

    public async Task<Result<TitanPosInfo>> GetInfoAsync(CancellationToken ct = default)
    {
        var response = await SendAsync<TitanInfoResponse>("info", new { }, ct);
        if (!response.IsSuccess)
            return Result<TitanPosInfo>.Failure(response.Error!);

        var data = response.Value!;
        return Result<TitanPosInfo>.Ok(new TitanPosInfo(
            ShiftOpened: data.ShiftOpened,
            Serial: data.Serial,
            Version: data.Version,
            IsBlocked: data.IsBlocked,
            BlockedText: data.BlockedText
        ));
    }

    // ── Отправка ──────────────────────────────────────────────────────────────

    private async Task<Result<T>> SendAsync<T>(
        string operation, object body, CancellationToken ct) where T : TitanBaseResponse
    {
        await _lock.WaitAsync(ct);
        try
        {
            var bodyJson = JsonSerializer.Serialize(body);
            var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(bodyJson)!;
            dict["TitanKey"] = _titanKey;
            var payloadJson = JsonSerializer.Serialize(dict);

            Log($"→ op={operation} body={payloadJson}");

            var httpContent = new StringContent(payloadJson, Encoding.UTF8);
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var httpResponse = await _http.PostAsync(operation, httpContent, ct);
            var content = await httpResponse.Content.ReadAsStringAsync(ct);

            Log($"← op={operation} http={httpResponse.StatusCode} body={content}");

            T? result;
            try
            {
                result = JsonSerializer.Deserialize<T>(content, JsonOptions);
            }
            catch (Exception ex)
            {
                return Result<T>.Failure($"Ошибка разбора ответа TitanPOS: {ex.Message}");
            }

            if (result == null)
                return Result<T>.Failure("Пустой ответ от TitanPOS");

            if (result.Status == 0)
            {
                Log($"✓ op={operation} новый ключ=[{result.TitanKey}]");
                if (!string.IsNullOrEmpty(result.TitanKey))
                    _titanKey = result.TitanKey;
                return Result<T>.Ok(result);
            }

            Log($"✗ op={operation} status={result.Status} error=[{result.Error}]");
            return Result<T>.Failure($"TitanPOS ошибка {result.Status}: {result.Error}");
        }
        finally
        {
            _lock.Release();
        }
    }

    // ── Лог ───────────────────────────────────────────────────────────────────

    private static void Log(string msg)
    {
        lock (_logLock)
            File.AppendAllText(@"D:\NewPos\scanner.log",
                $"{DateTime.Now:HH:mm:ss.fff} [TitanPOS] {msg}\r\n",
                new UTF8Encoding(false));
    }

    // ── Response модели ───────────────────────────────────────────────────────

    private class TitanBaseResponse
    {
        public string TitanKey { get; init; } = string.Empty;
        public int Status { get; init; }
        public string? Error { get; init; }
    }

    private class TitanCheckResponse : TitanBaseResponse
    {
        public int Number { get; init; }
        public string Position { get; init; } = string.Empty;
        public string UID { get; init; } = string.Empty;
        public string? ElCheckUrl { get; init; }
    }

    private class TitanInfoResponse : TitanBaseResponse
    {
        public bool ShiftOpened { get; init; }
        public string Serial { get; init; } = string.Empty;
        public string Version { get; init; } = string.Empty;
        public bool IsBlocked { get; init; }
        public string? BlockedText { get; init; }
    }
}