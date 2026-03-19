using MarkPos.Application;
using MarkPos.Application.DTOs;
using MarkPos.Application.Interfaces;
using MarkPos.Application.Session;
using MarkPos.Infrastructure;
using Microsoft.OpenApi;


var builder = WebApplication.CreateBuilder(args);

// ── Swagger ───────────────────────────────────────────────────────────────────

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── Infrastructure ────────────────────────────────────────────────────────────
var config = builder.Configuration;

builder.Services.AddMarkPosInfrastructure(
    connectionString: config["Database:MainConnection"]!,
    discountDbConnection: config["Database:DiscountConnection"]!,
    discountUrl: config["Discount:Url"]!,
    titanPosUrl: config["TitanPos:Url"]!,
    titanInitialKey: config["TitanPos:InitialKey"]!,
    stationConfig: new StationConfig
    {
        ShopNumber = config["Station:ShopNumber"]!,
        StationNumber = config["Station:StationNumber"]!,
        FiscalType = config["Station:FiscalType"]!,
        CashboxType = config["Station:CashboxType"]!,
        StationSaleTypeId = config["Station:StationSaleTypeId"]!,
        BaseId = config["Station:BaseId"]!
    },
    scannerPort: int.Parse(config["Scanner:Port"]!)
);

var app = builder.Build();

// ── Swagger UI ────────────────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MarkPos API v1");
    c.RoutePrefix = string.Empty;
});

// ── Session endpoints ─────────────────────────────────────────────────────────

// GET /session — текущее состояние
app.MapGet("/session", (IPosSession session) =>
    Results.Ok(session.State))
    .WithName("GetSession")
    .WithSummary("Получить текущее состояние сессии")
    .WithTags("Session");

// POST /session/scan — сканировать товар
app.MapPost("/session/scan", async (ScanRequest req, IPosSession session) =>
{
    await session.ScanItemAsync(req.Barcode, req.Quantity);
    return Results.Ok(session.State);
})
.WithName("ScanItem")
.WithSummary("Сканировать штрихкод товара")
.WithTags("Session");

// DELETE /session/lines/{lineNumber} — удалить строку
app.MapDelete("/session/lines/{lineNumber:int}", async (int lineNumber, IPosSession session) =>
{
    await session.RemoveItemAsync(lineNumber);
    return Results.Ok(session.State);
})
.WithName("RemoveItem")
.WithSummary("Удалить товар из чека")
.WithTags("Session");

// PUT /session/lines/{lineNumber}/quantity — изменить количество
app.MapPut("/session/lines/{lineNumber:int}/quantity", async (
    int lineNumber, AdjustQtyRequest req, IPosSession session) =>
{
    await session.AdjustQuantityAsync(lineNumber, req.Quantity);
    return Results.Ok(session.State);
})
.WithName("AdjustQuantity")
.WithSummary("Изменить количество товара")
.WithTags("Session");

// POST /session/card — привязать дисконтную карту
app.MapPost("/session/card", async (CardRequest req, IPosSession session) =>
{
    await session.AttachCardAsync(req.CardNumber);
    return Results.Ok(session.State);
})
.WithName("AttachCard")
.WithSummary("Привязать дисконтную карту «Е-плюс»")
.WithTags("Session");

// POST /session/pay — оплатить
app.MapPost("/session/pay", async (IPosSession session) =>
{
    var result = await session.PayAsync();
    return result.IsSuccess
        ? Results.Ok(new
        {
            result.Value!.DocNumber,
            result.Value.ElCheckUrl,
            Message = "Чек успешно закрыт"
        })
        : Results.BadRequest(new { Error = result.Error });
})
.WithName("Pay")
.WithSummary("Провести оплату и закрыть чек")
.WithTags("Session");

// POST /session/cancel — отменить чек
app.MapPost("/session/cancel", (IPosSession session) =>
{
    session.Cancel();
    return Results.Ok(new { Message = "Чек отменён" });
})
.WithName("Cancel")
.WithSummary("Отменить текущий чек")
.WithTags("Session");

// ── TitanPOS endpoints ────────────────────────────────────────────────────────

// GET /titanpos/info — статус кассы
app.MapGet("/titanpos/info", async (ITitanPosClient titan) =>
{
    var result = await titan.GetInfoAsync();
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.BadRequest(new { Error = result.Error });
})
.WithName("GetTitanInfo")
.WithSummary("Получить статус TitanPOS")
.WithTags("TitanPOS");

// POST /titanpos/shift/open — открыть смену
app.MapPost("/titanpos/shift/open", async (ITitanPosClient titan) =>
{
    var result = await titan.OpenShiftAsync();
    return result.IsSuccess
        ? Results.Ok(new { Message = "Смена открыта" })
        : Results.BadRequest(new { Error = result.Error });
})
.WithName("OpenShift")
.WithSummary("Открыть кассовую смену")
.WithTags("TitanPOS");

// POST /titanpos/shift/close — закрыть смену (Z-отчёт)
app.MapPost("/titanpos/shift/close", async (ITitanPosClient titan) =>
{
    var result = await titan.CloseShiftAsync();
    return result.IsSuccess
        ? Results.Ok(new { Message = "Смена закрыта (Z-отчёт)" })
        : Results.BadRequest(new { Error = result.Error });
})
.WithName("CloseShift")
.WithSummary("Закрыть кассовую смену (Z-отчёт)")
.WithTags("TitanPOS");

app.Run();

// ── Request DTOs ──────────────────────────────────────────────────────────────
record ScanRequest(string Barcode, decimal Quantity = 1);
record CardRequest(string CardNumber);
record AdjustQtyRequest(decimal Quantity);