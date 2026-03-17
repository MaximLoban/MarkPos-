using MarkPos.Application.Interfaces;
using MarkPos.Application.Scanner;
using MarkPos.Application.Session;
using MarkPos.Domain.Entities;
using MarkPos.UI.Commands;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace MarkPos.UI;

public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IPosSession _session;
    private readonly IScannerService _scanner;

    public MainViewModel(IPosSession session, IScannerService scanner)
    {
        _session = session;
        _scanner = scanner;

        _session.StateChanged += OnStateChanged;
        _scanner.MessageReceived += OnScannerMessage;

        ScanCommand = new AsyncRelayCommand(ScanAsync);
        RemoveCommand = new AsyncRelayCommand<int>(ln => _session.RemoveItemAsync(ln));
        PayCommand = new AsyncRelayCommand(PayAsync);
        CancelCommand = new RelayCommand(ConfirmAndCancel);
    }

    // ── Commands ───────────────────────────────────────────────────────────────

    public ICommand ScanCommand { get; }
    public ICommand RemoveCommand { get; }
    public ICommand PayCommand { get; }
    public ICommand CancelCommand { get; }

    // ── Bindable props ─────────────────────────────────────────────────────────

    private PosState _state = PosState.Empty;

    public IReadOnlyList<ReceiptLine> Lines => _state.Lines;
    public string TotalText => $"{_state.TotalSum:F2} BYN";
    public string DiscountText => $"Скидка: -{_state.DiscountSum:F2} BYN";
    public bool HasDiscount => _state.HasDiscount;
    public string StatusText => _state.Message ?? string.Empty;

    private string _barcodeInput = string.Empty;
    public string BarcodeInput
    {
        get => _barcodeInput;
        set { _barcodeInput = value; OnPropertyChanged(); }
    }

    // ── Handlers ───────────────────────────────────────────────────────────────

    private async Task ScanAsync()
    {
        var barcode = BarcodeInput.Trim();
        if (string.IsNullOrEmpty(barcode)) return;
        BarcodeInput = string.Empty;
        await _session.ScanItemAsync(barcode);
    }

    private async Task PayAsync()
    {
        var result = await _session.PayAsync();
        if (result.IsSuccess)
            MessageBox.Show(
                $"Чек #{result.Value!.DocNumber} закрыт успешно.",
                "Оплата принята", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ConfirmAndCancel()
    {
        var ok = MessageBox.Show(
            "Отменить текущий чек?", "Подтверждение",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (ok == MessageBoxResult.Yes)
            _session.Cancel();
    }

    private void OnStateChanged(PosState state)
    {
        _state = state;
        OnPropertyChanged(nameof(Lines));
        OnPropertyChanged(nameof(TotalText));
        OnPropertyChanged(nameof(DiscountText));
        OnPropertyChanged(nameof(HasDiscount));
        OnPropertyChanged(nameof(StatusText));
    }

    private void OnScannerMessage(ScannerMessage message)
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            switch (message)
            {
                case BarcodeMessage m:
                    await _session.ScanItemAsync(m.Barcode);
                    break;
                case WeightBarcodeMessage m:
                    await _session.ScanItemAsync(m.Barcode, m.Quantity);
                    break;
                case DiscountCardMessage m:
                    await _session.AttachCardAsync(m.CardNumber);
                    break;
                case AdvertisingQrMessage:
                    _state = _state with { Message = "Данный QR содержит рекламу." };
                    OnPropertyChanged(nameof(StatusText));
                    break;
                case InvalidBarcodeMessage m:
                    _state = _state with { Message = m.Reason };
                    OnPropertyChanged(nameof(StatusText));
                    break;
            }
        });
    }

    public void Dispose()
    {
        _session.StateChanged -= OnStateChanged;
        _scanner.MessageReceived -= OnScannerMessage;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}