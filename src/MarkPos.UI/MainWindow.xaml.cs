namespace MarkPos.UI;

using MarkPos.Application.DTOs;
using MarkPos.Application.UseCases;
using MarkPos.Domain.Entities;
using System.Windows;
using System.Windows.Input;



public partial class MainWindow : Window
{
    private readonly AddItemByBarcodeUseCase _addItem;
    private readonly RequestDiscountsUseCase _requestDiscounts;
    private readonly CloseReceiptUseCase _closeReceipt;

    private Receipt _receipt = Receipt.New(1); // TODO: заменить на счётчик из БД
    private CancellationTokenSource? _discountCts;

    public MainWindow(
        AddItemByBarcodeUseCase addItem,
        RequestDiscountsUseCase requestDiscounts,
        CloseReceiptUseCase closeReceipt)
    {
        InitializeComponent();
        _addItem = addItem;
        _requestDiscounts = requestDiscounts;
        _closeReceipt = closeReceipt;

        BarcodeInput.Focus();
    }

    // ── Сканирование ──────────────────────────────────────────────────────────

    private async void BarcodeInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            await AddByBarcodeAsync();
    }

    private async void AddButton_Click(object sender, RoutedEventArgs e)
        => await AddByBarcodeAsync();

    private async Task AddByBarcodeAsync()
    {
        var barcode = BarcodeInput.Text.Trim();
        if (string.IsNullOrEmpty(barcode)) return;

        StatusText.Text = "";
        BarcodeInput.Clear();

        var result = await _addItem.ExecuteAsync(_receipt, barcode);

        if (!result.IsSuccess)
        {
            StatusText.Text = result.Error;
            BarcodeInput.Focus();
            return;
        }

        RefreshGrid();
        BarcodeInput.Focus();

        // Запрашиваем скидки асинхронно — UI не блокируется
        await RequestDiscountsAsync();
    }

    // ── Скидки ───────────────────────────────────────────────────────────────

    private async Task RequestDiscountsAsync()
    {
        // Отменяем предыдущий запрос если пользователь быстро сканирует
        _discountCts?.Cancel();
        _discountCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(200, _discountCts.Token); // debounce
            await _requestDiscounts.ExecuteAsync(_receipt, _discountCts.Token);
            RefreshGrid();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            StatusText.Text = $"Скидки недоступны: {ex.Message}";
        }
    }

    // ── Удаление позиции ──────────────────────────────────────────────────────

    private async void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (LinesGrid.SelectedItem is not ReceiptLine line) return;

        _receipt.RemoveItem(line.LineNumber);
        RefreshGrid();
        await RequestDiscountsAsync();
    }

    // ── Отмена чека ───────────────────────────────────────────────────────────

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            "Отменить текущий чек?", "Подтверждение",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        _receipt = Receipt.New(1); // TODO: счётчик из БД
        RefreshGrid();
        StatusText.Text = "";
        BarcodeInput.Focus();
    }

    // ── Оплата ───────────────────────────────────────────────────────────────

    private async void PayButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_receipt.Lines.Any())
        {
            StatusText.Text = "Нет товаров в чеке";
            return;
        }

        // MVP: оплата наличными на полную сумму
        var payments = new List<TitanPayment>
        {
            new(Sum: _receipt.TotalSum, TypeFlag: 1)
        };

        var result = await _closeReceipt.ExecuteAsync(_receipt, payments);

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

        _receipt = Receipt.New(1); // TODO: счётчик из БД
        RefreshGrid();
        BarcodeInput.Focus();
    }

    // ── Обновление UI ─────────────────────────────────────────────────────────

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
}