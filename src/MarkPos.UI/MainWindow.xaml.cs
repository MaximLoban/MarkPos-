using LottieSharp.WPF;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace MarkPos.UI;

public partial class MainWindow : Window
{
    private LottieAnimationView? _lottie;
    private readonly MainViewModel _vm;
    private object? _lastCurrentItem;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        DataContext = _vm;
        _vm.PropertyChanged += OnViewModelPropertyChanged;

        Loaded += (_, _) => BarcodeInput.Focus();
        KeyDown += OnWindowKeyDown;
    }

    private void OnViewModelPropertyChanged(object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainViewModel.HasSuccess):
                if (_vm.HasSuccess) StartLottie();
                else StopLottie();
                break;

            case nameof(MainViewModel.CurrentItem):
                AnimateCurrentItemCard();
                break;
        }
    }

    private void AnimateCurrentItemCard()
    {
        // Анимируем только когда появляется новый товар
        if (_vm.CurrentItem == null) return;
        if (_vm.CurrentItem == _lastCurrentItem) return;
        _lastCurrentItem = _vm.CurrentItem;

        if (CurrentItemCard == null) return;

        // Сброс
        CurrentItemCard.Opacity = 0;
        var transform = new System.Windows.Media.TranslateTransform(0, -20);
        CurrentItemCard.RenderTransform = transform;

        // Анимация Y: -20 → 0
        var slideAnim = new DoubleAnimation(-20, 0, new Duration(TimeSpan.FromMilliseconds(300)))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        // Анимация Opacity: 0 → 1
        var fadeAnim = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(300)))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        transform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slideAnim);
        CurrentItemCard.BeginAnimation(OpacityProperty, fadeAnim);
    }

    private void StartLottie()
    {
        if (LottieContainer == null) return;

        var path = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Assets", "payment_success.json");

        File.AppendAllText(@"D:\NewPos\scanner.log",
            $"{DateTime.Now:HH:mm:ss} Lottie path=[{path}] exists=[{File.Exists(path)}]\r\n");

        if (!File.Exists(path)) return;

        _lottie = new LottieAnimationView
        {
            FileName = path,
            AutoPlay = true,
            RepeatCount = 0,
            Width = 500,
            Height = 500,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        LottieContainer.Content = _lottie;
    }

    private void StopLottie()
    {
        if (_lottie != null)
        {
            LottieContainer.Content = null;
            _lottie = null;
        }
    }

    private void BarcodeInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            _vm.ScanCommand.Execute(null);
    }

    private void OnWindowKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter && e.Key != Key.Tab)
            BarcodeInput.Focus();
    }

    protected override void OnClosed(EventArgs e)
    {
        _vm.PropertyChanged -= OnViewModelPropertyChanged;
        _vm.Dispose();
        base.OnClosed(e);
    }
}