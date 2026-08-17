using SerialDebugStudio.Desktop.ViewModels;
using Wpf.Ui.Controls;

namespace SerialDebugStudio.Desktop;

public partial class MainWindow : FluentWindow
{
    public MainWindow(
        MainWindowViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
    }
}