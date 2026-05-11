using System.Windows;
using FileNoteManager.UI.ViewModels;

namespace FileNoteManager.UI.Views;

/// <summary>
/// Quick-edit popup launched by the Shell context menu via --path argument.
/// </summary>
public partial class QuickNoteWindow : Window
{
    public QuickNoteWindow(QuickNoteViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.RequestClose += () => Close();

        Loaded += (_, _) => ContentBox.Focus();
    }

    /// <summary>
    /// Load the given file/folder path into the editor.
    /// Called from App.xaml.cs after DI resolution.
    /// </summary>
    public void LoadPath(string path)
    {
        if (DataContext is QuickNoteViewModel vm)
            vm.LoadPath(path);
    }
}
