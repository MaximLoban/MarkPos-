using MarkPos.Application;
using MarkPos.Application.Interfaces;
using MarkPos.Application.Session;
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

            services.AddTransient<MainViewModel>();
            services.AddTransient<MainWindow>();

            Services = services.BuildServiceProvider();

            // ── Запуск сканера ────────────────────────────────────────────────
            var scanner = Services.GetRequiredService<TcpScannerListener>();
            var cts = new CancellationTokenSource();
            _ = scanner.StartAsync(cts.Token);

            // ── Инициализация TitanPOS ────────────────────────────────────────
            var titanPos = Services.GetRequiredService<ITitanPosClient>();

            var initResult = await titanPos.InitAsync();
            if (!initResult.IsSuccess)
            {
                ShowError($"TitanPOS недоступен: {initResult.Error}\n\nПроверьте что TitanPOS запущен на {config["TitanPos:Url"]}");
                Shutdown();
                return;
            }

            var openResult = await titanPos.OpenSessionAsync(
                pin: config["TitanPos:CashierPin"]!,
                cashierName: config["TitanPos:CashierName"]!
            );
            if (!openResult.IsSuccess)
            {
                ShowError($"Не удалось открыть сеанс: {openResult.Error}");
                Shutdown();
                return;
            }

            // Открываем смену если она ещё не открыта
            // Открываем/переоткрываем смену если нужно
            var infoResult = await titanPos.GetInfoAsync();
            if (!infoResult.IsSuccess)
            {
                ShowError($"Не удалось получить статус TitanPOS: {infoResult.Error}");
                Shutdown();
                return;
            }

            var info = infoResult.Value!;

            // Смена открыта более 24 часов — закрываем и открываем новую
            if (info.IsBlocked)
            {
                File.AppendAllText(@"D:\NewPos\scanner.log",
                    $"{DateTime.Now:HH:mm:ss} Смена заблокирована (>24ч) — переоткрываем\r\n",
                    new UTF8Encoding(false));

                var closeShiftResult = await titanPos.CloseShiftAsync();
                if (!closeShiftResult.IsSuccess)
                {
                    ShowError($"Не удалось закрыть смену: {closeShiftResult.Error}");
                    Shutdown();
                    return;
                }

                var openShiftResult = await titanPos.OpenShiftAsync();
                if (!openShiftResult.IsSuccess)
                {
                    ShowError($"Не удалось открыть новую смену: {openShiftResult.Error}");
                    Shutdown();
                    return;
                }
            }
            else if (!info.ShiftOpened)
            {
                var shiftResult = await titanPos.OpenShiftAsync();
                if (!shiftResult.IsSuccess)
                {
                    ShowError($"Не удалось открыть смену: {shiftResult.Error}");
                    Shutdown();
                    return;
                }
            }
            // ─────────────────────────────────────────────────────────────────

            File.AppendAllText(@"D:\NewPos\scanner.log",
                $"{DateTime.Now:HH:mm:ss} TitanPOS готов, смена открыта\r\n",
                new UTF8Encoding(false));

            await Task.Delay(500);

            var welcomeWindow = new WelcomeWindow();
            welcomeWindow.Show();
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
            Width = 900,
            Height = 500,
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };

        var textBox = new System.Windows.Controls.TextBox
        {
            Text = message,
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,          // ← перенос строк
            AcceptsReturn = true,                      // ← поддержка многострочности
            IsUndoEnabled = false,
            VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Disabled,
            FontFamily = new System.Windows.Media.FontFamily("Consolas"),
            FontSize = 13,
            Margin = new Thickness(10),
            Background = System.Windows.Media.Brushes.WhiteSmoke
        };

        window.Content = textBox;
        window.Loaded += (_, _) =>
        {
            textBox.Focus();
            textBox.SelectAll();
        };
        window.ShowDialog();
    }
}