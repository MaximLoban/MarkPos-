using MarkPos.Application;
using MarkPos.Application.Interfaces;
using MarkPos.Application.Scanner;
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
        int scannerPort)
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
        });

        // TitanPOS HTTP client
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

        // Station config
        services.AddSingleton(stationConfig);

        // Use Cases
        services.AddTransient<AddItemByBarcodeUseCase>();
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
      

        return services;
    }
}