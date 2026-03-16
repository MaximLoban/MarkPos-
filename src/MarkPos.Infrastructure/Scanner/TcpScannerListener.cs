using MarkPos.Application.Scanner;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MarkPos.Infrastructure.Scanner;

public class TcpScannerListener : BackgroundService
{
    private readonly int _port;
    private readonly ScannerParser _parser;
    private readonly ILogger<TcpScannerListener> _logger;
    private static readonly object _logLock = new();

    public event Action<ScannerMessage>? MessageReceived;

    public TcpScannerListener(int port, ScannerParser parser, ILogger<TcpScannerListener> logger)
    {
        _port = port;
        _parser = parser;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        LogToFile($"ExecuteAsync запущен, порт {_port}");
        try
        {
            var listener = new TcpListener(IPAddress.Any, _port);
            listener.Start();
            LogToFile($"TcpListener запущен на порту {_port}");

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    LogToFile("Ожидаем подключения...");
                    var client = await listener.AcceptTcpClientAsync(ct);
                    LogToFile("MarketPosDevice подключился!");
                    _ = HandleClientAsync(client, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    LogToFile($"Ошибка при ожидании: {ex.Message}");
                    await Task.Delay(1000, ct);
                }
            }

            listener.Stop();
        }
        catch (Exception ex)
        {
            LogToFile($"КРИТИЧЕСКАЯ ОШИБКА: {ex}");
            throw;
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        using var _ = client;
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);

                if (line == null)
                {
                    LogToFile("MarketPosDevice отключился");
                    break;
                }

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                LogToFile($"Получена строка: [{line}]");
                LogToFile($"Подписчиков: {MessageReceived?.GetInvocationList().Length ?? 0}");

                var message = _parser.Parse(line);
                LogToFile($"Распознано: {message.GetType().Name}");

                MessageReceived?.Invoke(message);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogToFile($"Соединение прервано: {ex.Message}");
        }
    }

    private static void LogToFile(string message)
    {
        lock (_logLock)
        {
            File.AppendAllText(@"D:\NewPos\scanner.log",
                $"{DateTime.Now:HH:mm:ss.fff} {message}\r\n",
                new UTF8Encoding(false));
        }
    }
}