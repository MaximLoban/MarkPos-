using MarkPos.Application;
using MarkPos.Application.Interfaces;
using MarkPos.Application.Sales;
using MarkPos.Application.Scanner;
using MarkPos.Application.Session;      // ← НОВЫЙ using
using MarkPos.Application.UseCases;
using MarkPos.Infrastructure.Discount;
using MarkPos.Infrastructure.Persistence;
using MarkPos.Infrastructure.Scanner;
using MarkPos.Infrastructure.TitanPos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MarkPos.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMarkPosInfrastructure(
        this IServiceCollection services,
        string connectionString,
        string discountUrl,
        string titanPosUrl,
        string titanInitialKey,
        string discountDbConnection,
        StationConfig stationConfig,
        int scannerPort,
        bool fiscalEnabled = true)
    {
        // Persistence
        services.AddSingleton<IProductRepository>(
            _ => new ProductRepository(connectionString));

        services.AddSingleton<IDiscountCardRepository>(
            _ => new DiscountCardRepository(discountDbConnection));
        services.AddTransient<AttachDiscountCardUseCase>();

        // Discount HTTP client
        services.AddHttpClient<IDiscountClient, DiscountHttpClient>(client =>
        {
            client.BaseAddress = new Uri(discountUrl);
            client.Timeout = TimeSpan.FromSeconds(5);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        });

        // TitanPOS HTTP client
        if (fiscalEnabled)
        {
            services.AddSingleton<ITitanPosClient>(sp =>
            {
                var httpClient = sp.GetRequiredService<IHttpClientFactory>()
                    .CreateClient("TitanPos");
                return new TitanPosHttpClient(httpClient, titanInitialKey);
            });
            services.AddHttpClient("TitanPos", client =>
            {
                client.BaseAddress = new Uri(titanPosUrl);
                client.Timeout = TimeSpan.FromSeconds(10);
            });
        }
        else
        {
            services.AddSingleton<ITitanPosClient, NullFiscalClient>();
        }

        // Station config
        services.AddSingleton(stationConfig);

        // Use Cases
        services.AddTransient<AddItemByBarcodeUseCase>();
        services.AddTransient<RemoveItemUseCase>();          // ← НОВОЕ
        services.AddTransient<SearchProductsUseCase>();
        services.AddTransient<RequestDiscountsUseCase>();
        services.AddTransient<CloseReceiptUseCase>();

        // Scanner
        services.AddSingleton<ScannerParser>();
        services.AddSingleton<TcpScannerListener>(sp => new TcpScannerListener(
            port: scannerPort,
            parser: sp.GetRequiredService<ScannerParser>(),
            logger: sp.GetRequiredService<ILogger<TcpScannerListener>>()
        ));
        services.AddSingleton<IScannerService, TcpScannerService>(); // ← НОВОЕ

        var cs = connectionString;
        services.AddSingleton<IReceiptRepository>(_ => new ReceiptRepository(cs));
        services.AddSingleton<ICatalogRepository>(_ => new CatalogRepository(cs));

        // Session facade
        services.AddSingleton<IPosSession, PosSession>();



        return services;
    }
}