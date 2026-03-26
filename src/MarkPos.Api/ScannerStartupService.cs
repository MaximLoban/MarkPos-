using MarkPos.Application.Scanner;
using MarkPos.Application.Session;
using MarkPos.Infrastructure.Scanner;

public class ScannerStartupService : IHostedService
{
    private readonly TcpScannerListener _scanner;
    private readonly IPosSession _session;

    public ScannerStartupService(
        TcpScannerListener scanner,
        IPosSession session)
    {
        _scanner = scanner;
        _session = session;
    }

    public Task StartAsync(CancellationToken ct)
    {
        _scanner.MessageReceived += OnMessage;
        Console.WriteLine("ScannerStartupService subscribed");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct)
    {
        _scanner.MessageReceived -= OnMessage;
        return Task.CompletedTask;
    }

    private async void OnMessage(ScannerMessage message)
    {
        Console.WriteLine($"HANDLER: {message.GetType().Name}");

        try
        {
            switch (message)
            {
                case BarcodeMessage b:
                    Console.WriteLine($"SCAN → {b.Barcode}");
                    await _session.ScanItemAsync(b.Barcode, 1);
                    break;

                case WeightBarcodeMessage w:
                    Console.WriteLine($"WEIGHT → {w.Barcode} {w.Quantity}");
                    await _session.ScanItemAsync(w.Barcode, w.Quantity);
                    break;

                case DiscountCardMessage card:
                    Console.WriteLine($"CARD → {card.CardNumber}");
                    await _session.AttachCardAsync(card.CardNumber);
                    break;

                case InvalidBarcodeMessage invalid:
                    Console.WriteLine($"INVALID: {invalid.Raw}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex}");
        }
    }
}