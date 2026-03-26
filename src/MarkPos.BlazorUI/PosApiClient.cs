using System.Net.Http.Json;

namespace MarkPos.BlazorUI;

public class PosApiClient
{
    private readonly HttpClient _http;

    public PosApiClient(HttpClient http) => _http = http;

    public Task<PosStateDto?> GetSessionAsync()
        => _http.GetFromJsonAsync<PosStateDto>("session");

    public async Task<PosStateDto?> ScanAsync(string barcode, decimal quantity = 1)
    {
        var r = await _http.PostAsJsonAsync("session/scan", new { barcode, quantity });
        return await r.Content.ReadFromJsonAsync<PosStateDto>();
    }

    public async Task<PosStateDto?> RemoveLineAsync(int lineNumber)
    {
        var r = await _http.DeleteAsync($"session/lines/{lineNumber}");
        return await r.Content.ReadFromJsonAsync<PosStateDto>();
    }

    public async Task<PosStateDto?> ChangeQtyAsync(int lineNumber, decimal qty)
    {
        var r = await _http.PutAsJsonAsync($"session/lines/{lineNumber}/quantity", new { quantity = qty });
        return await r.Content.ReadFromJsonAsync<PosStateDto>();
    }

    public async Task<PosStateDto?> AttachCardAsync(string cardNumber)
    {
        var r = await _http.PostAsJsonAsync("session/card", new { cardNumber });
        return await r.Content.ReadFromJsonAsync<PosStateDto>();
    }

    public async Task<PayResultDto?> PayAsync()
    {
        var r = await _http.PostAsync("session/pay", null);
        if (!r.IsSuccessStatusCode)
        {
            try { return await r.Content.ReadFromJsonAsync<PayResultDto>(); }
            catch { return new PayResultDto { Error = $"Ошибка сервера ({(int)r.StatusCode})" }; }
        }
        return await r.Content.ReadFromJsonAsync<PayResultDto>();
    }

    public Task CancelAsync() => _http.PostAsync("session/cancel", null);

    public Task<List<GoodsGroupDto>?> GetCatalogGroupsAsync()
        => _http.GetFromJsonAsync<List<GoodsGroupDto>>("catalog/groups");

    public Task<List<GoodsGroupDto>?> GetChildGroupsAsync(long groupId)
        => _http.GetFromJsonAsync<List<GoodsGroupDto>>($"catalog/groups/{groupId}/children");

    public async Task<bool> GroupHasChildrenAsync(long groupId)
    {
        var r = await _http.GetFromJsonAsync<bool>($"catalog/groups/{groupId}/has-children");
        return r;
    }

    public Task<List<CatalogProductDto>?> GetGroupItemsAsync(long groupId)
        => _http.GetFromJsonAsync<List<CatalogProductDto>>($"catalog/groups/{groupId}/items");
}

public class PosStateDto
{
    public List<LineDto> Lines { get; set; } = new();
    public decimal TotalSum { get; set; }
    public decimal DiscountSum { get; set; }
    public string? Message { get; set; }
    public int MessageType { get; set; }
    public string? DiscountCardNumber { get; set; }
    public string? ElCheckUrl { get; set; }
}

public class LineDto
{
    public int LineNumber { get; set; }
    public ProductDto? Product { get; set; }
    public decimal PriceOld { get; set; }
    public decimal Quantity { get; set; }
    public decimal TotalSum { get; set; }
    public decimal DiscountSum { get; set; }
    public string Name => Product?.Name ?? "";
}

public class ProductDto
{
    public string Name { get; set; } = "";
    public long GoodsId { get; set; }
}

public class PayResultDto
{
    public int? DocNumber { get; set; }
    public string? ElCheckUrl { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}

public class GoodsGroupDto
{
    public long GoodsWeightGroupId { get; set; }
    public long DepartmentNumber { get; set; }
    public string Name { get; set; } = "";
    public string ImageUrl => $"images/GoodsGroup/GW{DepartmentNumber:D10}.png";
}

public class CatalogProductDto
{
    public long GoodsId { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public string ImageUrl => $"http://localhost:5050/products/{GoodsId}/image";
}