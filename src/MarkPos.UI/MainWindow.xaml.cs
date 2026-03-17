using System.Windows;
using System.Windows.Input;

namespace MarkPos.UI;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += (_, _) => BarcodeInput.Focus();
        Closed += (_, _) => viewModel.Dispose();
    }

    private void BarcodeInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is MainViewModel vm)
            vm.ScanCommand.Execute(null);
    }
}