using MarkPos.Application.Interfaces;
using MarkPos.Application.Scanner;
using MarkPos.Application.Session;
using MarkPos.Domain.Entities;
using MarkPos.UI.Commands;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MarkPos.UI;

public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IPosSession _session;
    private readonly IScannerService _scanner;
    private string _lastBarcode = string.Empty;

    public MainViewModel(IPosSession session, IScannerService scanner)
    {
        _session = session;
        _scanner = scanner;

        _session.StateChanged += OnStateChanged;
        _scanner.MessageReceived += OnScannerMessage;

        ScanCommand = new AsyncRelayCommand(ScanAsync);
        RemoveCommand = new AsyncRelayCommand<int>(ln => _session.RemoveItemAsync(ln));
        RemoveCurrentCommand = new AsyncRelayCommand(RemoveCurrentAsync);
        IncreaseQtyCommand = new AsyncRelayCommand(IncreaseQtyAsync);
        DecreaseQtyCommand = new AsyncRelayCommand(DecreaseQtyAsync);
        PayCommand = new AsyncRelayCommand(PayAsync);
        CancelCommand = new RelayCommand(ConfirmAndCancel);
        ClearErrorCommand = new RelayCommand(ClearError);
        ClearSuccessCommand = new RelayCommand(ClearSuccess);
    }

    // ── Commands ───────────────────────────────────────────────────────────────

    public ICommand ScanCommand { get; }
    public ICommand RemoveCommand { get; }
    public ICommand RemoveCurrentCommand { get; }
    public ICommand IncreaseQtyCommand { get; }
    public ICommand DecreaseQtyCommand { get; }
    public ICommand PayCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ClearErrorCommand { get; }
    public ICommand ClearSuccessCommand { get; }

    // ── Bindable props ─────────────────────────────────────────────────────────

    private PosState _state = PosState.Empty;

    public IReadOnlyList<ReceiptLine> Lines => _state.Lines;
    public string TotalText => $"{_state.TotalSum:F2}";
    public string SumWithoutDiscountText => $"{(_state.TotalSum + _state.DiscountSum):F2}";
    public string DiscountSumText => $"-{_state.DiscountSum:F2}";
    public bool HasDiscount => _state.HasDiscount;
    public bool HasItems => _state.HasItems;

    // Полноэкранная ошибка
    public bool HasError => _state.MessageType == PosMessageType.Error;
    public string ErrorMessage => _state.Message ?? string.Empty;
    public string ErrorBarcode => _lastBarcode;

    // Полноэкранный успех
    public bool HasSuccess => _state.MessageType == PosMessageType.Success;
    public string? ElCheckUrl => _state.ElCheckUrl;

    // Инфо-полоска
    public bool HasInfoMessage => _state.MessageType == PosMessageType.Info
                                                         || _state.MessageType == PosMessageType.Warning;
    public string InfoMessage => _state.Message ?? string.Empty;
    public bool IsInfoWarning => _state.MessageType == PosMessageType.Warning;

    // Дисконтная карта
    public bool HasDiscountCard => _state.DiscountCardNumber != null;
    public string DiscountCardText =>
        _state.DiscountCardNumber is null
            ? string.Empty
            : $"Дисконтная карта «Е-плюс» №••••••••{_state.DiscountCardNumber[^4..]}••• принята!";

    // Текущий товар
    public ReceiptLine? CurrentItem => _state.Lines.Count > 0 ? _state.Lines[^1] : null;
    public bool HasCurrentItem => CurrentItem != null;
    public string CurrentItemName => CurrentItem?.Product.Name ?? string.Empty;
    public string CurrentItemPrice => CurrentItem != null ? $"{CurrentItem.PriceOld:F2}" : "0.00";
    public string CurrentItemQty => CurrentItem != null ? $"{CurrentItem.Quantity:F0}" : "0";
    public string CurrentItemTotal => CurrentItem != null ? $"{CurrentItem.TotalSum:F2}" : "0.00";
    public string CurrentItemDiscount => CurrentItem is { DiscountSum: > 0 }
                                                            ? $"Скидка: -{CurrentItem.DiscountSum:F2}"
                                                            : string.Empty;
    public bool HasCurrentItemDiscount => CurrentItem is { DiscountSum: > 0 };
    public string? CurrentItemImagePath => CurrentItem != null
    ? GetImagePath(CurrentItem.Product.GoodsId)
    : null;

    private static string? GetImagePath(long goodsId)
    {
        var padded = goodsId.ToString("D8");          // 00936524
        var last4 = padded[^4..];                     // 6524
        var path = $@"D:\Image\GoodsImage\Goods\256\{last4}\{padded}\norm\{padded}.n_1.png";
        return System.IO.File.Exists(path) ? path : null;
    }
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
        _lastBarcode = barcode;
        BarcodeInput = string.Empty;
        await _session.ScanItemAsync(barcode);
    }

    private async Task RemoveCurrentAsync()
    {
        if (CurrentItem is null) return;
        await _session.RemoveItemAsync(CurrentItem.LineNumber);
    }

    private async Task IncreaseQtyAsync()
    {
        if (CurrentItem is null) return;
        var barcode = CurrentItem.Product.Barcode ?? CurrentItem.Product.Gtin;
        if (string.IsNullOrEmpty(barcode)) return;
        await _session.ScanItemAsync(barcode, 1);
    }

    private async Task DecreaseQtyAsync()
    {
        if (CurrentItem is null) return;
        if (CurrentItem.Quantity <= 1)
            await _session.RemoveItemAsync(CurrentItem.LineNumber);
        else
            await _session.AdjustQuantityAsync(CurrentItem.LineNumber, CurrentItem.Quantity - 1);
    }

    private async Task PayAsync()
    {
        var result = await _session.PayAsync();
        if (!result.IsSuccess) return;

        // Открываем электронный чек если есть
        if (!string.IsNullOrEmpty(result.Value!.ElCheckUrl))
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = result.Value.ElCheckUrl,
                    UseShellExecute = true
                });
            }
            catch { /* игнорируем */ }
        }
    }

    private void ConfirmAndCancel()
    {
        var ok = System.Windows.MessageBox.Show(
            "Отменить текущий чек?", "Подтверждение",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);
        if (ok == System.Windows.MessageBoxResult.Yes)
            _session.Cancel();
    }

    private void ClearError()
    {
        _lastBarcode = string.Empty;
        _state = _state with { Message = null, MessageType = PosMessageType.None };
        NotifyAll();
        BarcodeInput = string.Empty;
    }

    private void ClearSuccess()
    {
        _state = _state with { Message = null, MessageType = PosMessageType.None, ElCheckUrl = null };
        NotifyAll();
        BarcodeInput = string.Empty;
    }

    private void OnStateChanged(PosState state)
    {
        _state = state;
        NotifyAll();
    }

    private void NotifyAll()
    {
        OnPropertyChanged(nameof(Lines));
        OnPropertyChanged(nameof(TotalText));
        OnPropertyChanged(nameof(SumWithoutDiscountText));
        OnPropertyChanged(nameof(DiscountSumText));
        OnPropertyChanged(nameof(HasDiscount));
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(ErrorMessage));
        OnPropertyChanged(nameof(ErrorBarcode));
        OnPropertyChanged(nameof(HasSuccess));
        OnPropertyChanged(nameof(ElCheckUrl));
        OnPropertyChanged(nameof(HasInfoMessage));
        OnPropertyChanged(nameof(InfoMessage));
        OnPropertyChanged(nameof(IsInfoWarning));
        OnPropertyChanged(nameof(HasDiscountCard));
        OnPropertyChanged(nameof(DiscountCardText));
        OnPropertyChanged(nameof(CurrentItem));
        OnPropertyChanged(nameof(HasCurrentItem));
        OnPropertyChanged(nameof(CurrentItemName));
        OnPropertyChanged(nameof(CurrentItemPrice));
        OnPropertyChanged(nameof(CurrentItemQty));
        OnPropertyChanged(nameof(CurrentItemTotal));
        OnPropertyChanged(nameof(CurrentItemDiscount));
        OnPropertyChanged(nameof(HasCurrentItemDiscount));
        OnPropertyChanged(nameof(CurrentItemImagePath));

        // Автозакрытие экрана успеха через 5 секунд
        if (_state.MessageType == PosMessageType.Success)
        {
            Task.Delay(5000).ContinueWith(_ =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (_state.MessageType == PosMessageType.Success)
                        ClearSuccess();
                });
            });
        }
    }

    private void OnScannerMessage(ScannerMessage message)
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            switch (message)
            {
                case BarcodeMessage m:
                    _lastBarcode = m.Barcode;
                    await _session.ScanItemAsync(m.Barcode);
                    break;
                case WeightBarcodeMessage m:
                    _lastBarcode = m.Barcode;
                    await _session.ScanItemAsync(m.Barcode, m.Quantity);
                    break;
                case DiscountCardMessage m:
                    await _session.AttachCardAsync(m.CardNumber);
                    break;
                case AdvertisingQrMessage:
                    _state = _state with
                    {
                        Message = "Данный QR содержит рекламу.",
                        MessageType = PosMessageType.Info
                    };
                    NotifyAll();
                    break;
                case InvalidBarcodeMessage m:
                    _state = _state with
                    {
                        Message = m.Reason,
                        MessageType = PosMessageType.Warning
                    };
                    NotifyAll();
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