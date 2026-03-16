using MarkPos.Application.DTOs;
using MarkPos.Application.Interfaces;
using MarkPos.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;


namespace MarkPos.Infrastructure.Discount;

public class DiscountHttpClient : IDiscountClient
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
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

        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsJsonAsync("MarketPosDiscountPrice", envelope, ct);
        }
        catch (Exception ex)
        {
            return Result<DiscountResponse>.Failure($"Дисконтный модуль недоступен: {ex.Message}");
        }

        if (!response.IsSuccessStatusCode)
            return Result<DiscountResponse>.Failure(
                $"Дисконтный модуль вернул {(int)response.StatusCode}");

        var content = await response.Content.ReadAsStringAsync(ct);

        DiscountResponse? result;
        try
        {
            result = JsonSerializer.Deserialize<DiscountResponse>(content, JsonOptions);
        }
        catch (Exception ex)
        {
            return Result<DiscountResponse>.Failure($"Ошибка разбора ответа: {ex.Message}");
        }

        if (result == null)
            return Result<DiscountResponse>.Failure("Пустой ответ от дисконтного модуля");

        return Result<DiscountResponse>.Ok(result);
    }
}