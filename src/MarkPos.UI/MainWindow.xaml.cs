using MarkPos.Application.DTOs;
using MarkPos.Application.Scanner;
using MarkPos.Application.UseCases;
using MarkPos.Domain.Entities;
using MarkPos.Infrastructure.Scanner;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

namespace MarkPos.UI;

public partial class MainWindow : Window
{
    private readonly AddItemByBarcodeUseCase _addItem;
    private readonly RequestDiscountsUseCase _requestDiscounts;
    private readonly CloseReceiptUseCase _closeReceipt;
    private readonly TcpScannerListener _scanner;
    private readonly AttachDiscountCardUseCase _attachDiscountCard;

    private Receipt _receipt = Receipt.New(1);
    private CancellationTokenSource? _discountCts;

    public MainWindow(
        AddItemByBarcodeUseCase addItem,
        RequestDiscountsUseCase requestDiscounts,
        CloseReceiptUseCase closeReceipt,
        TcpScannerListener scanner,
        AttachDiscountCardUseCase attachDiscountCard)
    {
        InitializeComponent();
        _addItem = addItem;
        _requestDiscounts = requestDiscounts;
        _closeReceipt = closeReceipt;
        _scanner = scanner;
        _attachDiscountCard = attachDiscountCard;

        _scanner.MessageReceived += OnScannerMessage;

        BarcodeInput.Focus();
    }

    private void OnScannerMessage(ScannerMessage message)
    {
        LogToFile($"OnScannerMessage: {message.GetType().Name}");

        Dispatcher.InvokeAsync(async () =>
        {
            try
            {
                switch (message)
                {
                    case BarcodeMessage barcodeMsg:
                        LogToFile($"Barcode received: {barcodeMsg.Barcode}");
                        await AddByBarcodeAsync(barcodeMsg.Barcode);
                        break;

                    case WeightBarcodeMessage weightMsg:
                        LogToFile($"Weight barcode: {weightMsg.Barcode} qty={weightMsg.Quantity}");
                        await AddByBarcodeAsync(weightMsg.Barcode, weightMsg.Quantity);
                        break;

                    case DiscountCardMessage cardMsg:
                        LogToFile($"Discount card: {cardMsg.CardNumber}");
                        var cardResult = await _attachDiscountCard.ExecuteAsync(_receipt, cardMsg.CardNumber);
                        LogToFile($"Card attach result: {cardResult.IsSuccess} {cardResult.Error}");
                        if (cardResult.IsSuccess)
                            StatusText.Text = $"Карта принята: {cardMsg.CardNumber}";
                        else
                            StatusText.Text = cardResult.Error!;
                        break;

                    case AdvertisingQrMessage:
                        StatusText.Text = "Данный QR содержит рекламу.";
                        break;

                    case InvalidBarcodeMessage invalidMsg:
                        StatusText.Text = invalidMsg.Reason;
                        break;
                }
            }
            catch (Exception ex)
            {
                LogToFile($"ERROR in Dispatcher: {ex.Message}");
                StatusText.Text = $"Ошибка: {ex.Message}";
            }
        });
    }

    private async void BarcodeInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            await AddByBarcodeAsync(BarcodeInput.Text.Trim());
    }

    private async void AddButton_Click(object sender, RoutedEventArgs e)
        => await AddByBarcodeAsync(BarcodeInput.Text.Trim());

    private async Task AddByBarcodeAsync(string barcode, decimal quantity = 1)
    {
        if (string.IsNullOrEmpty(barcode)) return;

        LogToFile($"AddByBarcodeAsync: barcode={barcode} qty={quantity}");
        StatusText.Text = "";
        BarcodeInput.Clear();

        var result = await _addItem.ExecuteAsync(_receipt, barcode, quantity);

        LogToFile($"AddItem result: success={result.IsSuccess} error={result.Error}");

        if (!result.IsSuccess)
        {
            StatusText.Text = result.Error;
            BarcodeInput.Focus();
            return;
        }

        LogToFile($"Item added, lines count={_receipt.Lines.Count}, calling RequestDiscountsAsync");
        RefreshGrid();
        BarcodeInput.Focus();

        await RequestDiscountsAsync();
    }

    private async Task RequestDiscountsAsync()
    {
        LogToFile($"RequestDiscountsAsync called, lines={_receipt.Lines.Count}");
        _discountCts?.Cancel();
        _discountCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(200, _discountCts.Token);
            LogToFile("Sending request to discount module...");
            await _requestDiscounts.ExecuteAsync(_receipt, _discountCts.Token);
            LogToFile("Discount module response received");
            RefreshGrid();
        }
        catch (OperationCanceledException)
        {
            LogToFile("RequestDiscountsAsync cancelled");
        }
        catch (Exception ex)
        {
            LogToFile($"Discount module error: {ex.Message}");
            StatusText.Text = $"Скидки недоступны: {ex.Message}";
        }
    }

    private async void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (LinesGrid.SelectedItem is not ReceiptLine line) return;

        LogToFile($"RemoveItem: lineNumber={line.LineNumber}");
        _receipt.RemoveItem(line.LineNumber);
        RefreshGrid();
        await RequestDiscountsAsync();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            "Отменить текущий чек?", "Подтверждение",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        LogToFile("Receipt cancelled");
        _receipt = Receipt.New(1);
        RefreshGrid();
        StatusText.Text = "";
        BarcodeInput.Focus();
    }

    private async void PayButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_receipt.Lines.Any())
        {
            StatusText.Text = "Нет товаров в чеке";
            return;
        }

        LogToFile($"PayButton clicked, total={_receipt.TotalSum}");

        var payments = new List<TitanPayment>
        {
            new(Sum: _receipt.TotalSum, TypeFlag: 1)
        };

        var result = await _closeReceipt.ExecuteAsync(_receipt, payments);

        LogToFile($"CloseReceipt result: success={result.IsSuccess} error={result.Error}");

        if (!result.IsSuccess)
        {
            StatusText.Text = $"Ошибка оплаты: {result.Error}";
            return;
        }

        MessageBox.Show(
            $"Чек #{result.Value!.DocNumber} закрыт успешно.",
            "Оплата принята",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        _receipt = Receipt.New(1);
        RefreshGrid();
        BarcodeInput.Focus();
    }

    private void RefreshGrid()
    {
        LinesGrid.ItemsSource = null;
        LinesGrid.ItemsSource = _receipt.Lines;

        TotalText.Text = $"{_receipt.TotalSum:F2} BYN";

        if (_receipt.DiscountSum > 0)
            DiscountText.Text = $"Скидка: -{_receipt.DiscountSum:F2} BYN";
        else
            DiscountText.Text = "";
    }

    private static void LogToFile(string message)
    {
        File.AppendAllText(@"D:\NewPos\scanner.log",
            $"{DateTime.Now:HH:mm:ss.fff} {message}\r\n",
            new UTF8Encoding(false));
    }
}
