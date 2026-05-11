using System.Windows;
using FileNoteManager.UI.ViewModels;

namespace FileNoteManager.UI;

/// <summary>
/// Main application window. DataContext is MainViewModel injected via DI.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}