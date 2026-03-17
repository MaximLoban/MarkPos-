using MarkPos.Application.DTOs;
using MarkPos.Application.Interfaces;
using MarkPos.Domain.ValueObjects;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace MarkPos.Infrastructure.Discount;

public class DiscountHttpClient : IDiscountClient
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions SerializeOptions = new()
    {
        PropertyNamingPolicy = null
    };

    public DiscountHttpClient(HttpClient http)
        => _http = http;

    public async Task<Result<DiscountResponse>> RequestDiscountsAsync(
        DiscountRequest request,
        CancellationToken ct = default)
    {
        var envelope = new
        {
            CRC = "",
            Version = "1",
            Packet = new
            {
                TokenId = request.TokenId,
                FromId = "0",
                ServerKey = "0",
                UserName = "0",
                UserPassword = "0",
                Data = new
                {
                    CreditGroup = request.CreditGroup,
                    Credit = request.Credit,
                    PaymentInfo = Array.Empty<object>(),
                    BonusItems = Array.Empty<object>(),
                    DiscountCardId = request.DiscountCardId,
                    DiscountCardGroupId = request.DiscountCardGroupId,
                    DiscountCardGroupId2 = request.DiscountCardGroupId2
                }
            }
        };

        var json = JsonSerializer.Serialize(envelope, SerializeOptions);
        LogToFile($"URL: {_http.BaseAddress}MarketPosDiscountPrice");
        LogToFile($"Request: {json}");

        var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsync("/MarketPosDiscountPrice", httpContent, ct);
        }
        catch (Exception ex)
        {
            LogToFile($"HTTP error: {ex.Message}");
            return Result<DiscountResponse>.Failure($"Дисконтный модуль недоступен: {ex.Message}");
        }

        LogToFile($"HTTP status: {(int)response.StatusCode}");

        if (!response.IsSuccessStatusCode)
            return Result<DiscountResponse>.Failure(
                $"Дисконтный модуль вернул {(int)response.StatusCode}");

        var responseContent = await response.Content.ReadAsStringAsync(ct);
        LogToFile($"Response: {responseContent}");

        DiscountResponse? result;
        try
        {
            result = JsonSerializer.Deserialize<DiscountResponse>(responseContent, DeserializeOptions);
        }
        catch (Exception ex)
        {
            LogToFile($"Parse error: {ex.Message}");
            return Result<DiscountResponse>.Failure($"Ошибка разбора ответа: {ex.Message}");
        }

        if (result == null)
            return Result<DiscountResponse>.Failure("Пустой ответ от дисконтного модуля");

        return Result<DiscountResponse>.Ok(result);
    }

    private static void LogToFile(string message)
    {
        File.AppendAllText(@"D:\NewPos\scanner.log",
            $"{DateTime.Now:HH:mm:ss.fff} [DM] {message}\r\n",
            new UTF8Encoding(false));
    }
}