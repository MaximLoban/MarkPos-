using MarkPos.Application;
using MarkPos.Application.Interfaces;
using MarkPos.Infrastructure;
using MarkPos.Infrastructure.Persistence;
using MarkPos.Infrastructure.Scanner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text;
using System.Windows;

namespace MarkPos.UI;

public partial class App : System.Windows.Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (s, ex) =>
        {
            ShowError(ex.Exception.ToString());
            ex.Handled = true;
        };

        try
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var services = new ServiceCollection();

            services.AddLogging(builder => builder
                .AddConsole()
                .AddDebug()
                .SetMinimumLevel(LogLevel.Debug));

            services.AddMarkPosInfrastructure(
                connectionString: config["Database:MainConnection"]!,
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

            services.AddSingleton<IReceiptRepository, ReceiptRepository>();
            services.AddTransient<MainWindow>();

            Services = services.BuildServiceProvider();

            var scanner = Services.GetRequiredService<TcpScannerListener>();
            var cts = new CancellationTokenSource();
            _ = scanner.StartAsync(cts.Token);

            File.AppendAllText(@"D:\NewPos\scanner.log",
                $"{DateTime.Now:HH:mm:ss} StartAsync вызван\r\n",
                new UTF8Encoding(false));

            await Task.Delay(500);

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            ShowError(ex.ToString());
            Shutdown();
        }
    }

    private static void ShowError(string message)
    {
        var window = new Window
        {
            Title = "Ошибка",
            Width = 800,
            Height = 400,
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };

        var textBox = new System.Windows.Controls.TextBox
        {
            Text = message,
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
            Margin = new Thickness(10)
        };

        window.Content = textBox;
        window.ShowDialog();
    }
}