using MarkPos.Application.DTOs;
using MarkPos.Application.Interfaces;
using MarkPos.Domain.ValueObjects;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MarkPos.Infrastructure.TitanPos;

public class TitanPosHttpClient : ITitanPosClient
{
    private readonly HttpClient _http;
    private string _titanKey;

    // TitanPOS не поддерживает параллельные запросы — только один за раз
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TitanPosHttpClient(HttpClient http, string initialKey)
    {
        _http = http;
        _titanKey = initialKey;
    }

    public async Task<Result> InitAsync(CancellationToken ct = default)
    {
        var response = await SendAsync<TitanBaseResponse>("init", new { }, ct);
        return response.IsSuccess ? Result.Ok() : Result.Failure(response.Error!);
    }

    public async Task<Result> OpenSessionAsync(string pin, string cashierName, CancellationToken ct = default)
    {
        var response = await SendAsync<TitanBaseResponse>("open", new
        {
            Pin = pin,
            Cashier = cashierName
        }, ct);
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

    // ── Общий метод отправки ─────────────────────────────────────────────────

    private async Task<Result<T>> SendAsync<T>(
        string operation, object body, CancellationToken ct) where T : TitanBaseResponse
    {
        await _lock.WaitAsync(ct);
        try
        {
            // Добавляем текущий TitanKey к каждому запросу
            var payload = AddTitanKey(body);

            var httpResponse = await _http.PostAsJsonAsync(operation, payload, ct);
            var content = await httpResponse.Content.ReadAsStringAsync(ct);

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
                // Обновляем ключ только при успехе
                if (!string.IsNullOrEmpty(result.TitanKey))
                    _titanKey = result.TitanKey;

                return Result<T>.Ok(result);
            }

            return Result<T>.Failure($"TitanPOS ошибка {result.Status}: {result.Error}");
        }
        finally
        {
            _lock.Release();
        }
    }

    private object AddTitanKey(object body)
    {
        // Сериализуем body, добавляем TitanKey и десериализуем обратно в словарь
        var json = JsonSerializer.Serialize(body);
        var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json)!;
        dict["TitanKey"] = _titanKey;
        return dict;
    }

    // ── Response модели ──────────────────────────────────────────────────────

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