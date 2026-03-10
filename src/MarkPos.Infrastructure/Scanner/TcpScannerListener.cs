using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MarkPos.Application.Scanner;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;


namespace MarkPos.Infrastructure.Scanner;

/// <summary>
/// Фоновый TCP сервер на порту 60001.
/// MarketPosDevice подключается как клиент и присылает строки со штрихкодами.
/// При обрыве соединения — ждёт нового подключения.
/// </summary>
public class TcpScannerListener : BackgroundService
{
    private readonly int _port;
    private readonly ScannerParser _parser;
    private readonly ILogger<TcpScannerListener> _logger;

    /// <summary>
    /// Событие срабатывает когда от сканера пришло распознанное сообщение.
    /// UI подписывается на это событие.
    /// </summary>
    public event Action<ScannerMessage>? MessageReceived;

    public TcpScannerListener(int port, ScannerParser parser, ILogger<TcpScannerListener> logger)
    {
        _port = port;
        _parser = parser;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var listener = new TcpListener(IPAddress.Any, _port);
        listener.Start();
        _logger.LogInformation("TCP сканер слушает порт {Port}", _port);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var client = await listener.AcceptTcpClientAsync(ct);
                _logger.LogInformation("MarketPosDevice подключился");

                // Каждое подключение обрабатываем в отдельной задаче
                _ = HandleClientAsync(client, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при ожидании подключения сканера");
                await Task.Delay(1000, ct);
            }
        }

        listener.Stop();
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
                    _logger.LogInformation("MarketPosDevice отключился");
                    break;
                }

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                _logger.LogDebug("Получена строка от сканера: {Line}", line);

                var message = _parser.Parse(line);
                MessageReceived?.Invoke(message);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Соединение со сканером прервано: {Message}", ex.Message);
        }
    }
}