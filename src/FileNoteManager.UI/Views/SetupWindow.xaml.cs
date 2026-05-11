using System.Windows;
using System.Windows.Documents;
using FileNoteManager.Shell;

namespace FileNoteManager.UI.Views;

/// <summary>
/// First-run setup dialog. Shown automatically when the shell is not registered.
/// The user clicks one button to register the right-click context menu.
/// </summary>
public partial class SetupWindow : Window
{
    public SetupWindow()
    {
        InitializeComponent();
    }

    private void RegisterButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath))
            {
                MessageBox.Show("无法获取程序路径，请手动从主界面注册。",
                    "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                Close();
                return;
            }

            ShellRegistrar.Register(exePath);

            MessageBox.Show(
                "✅ 注册成功！\n\n现在在任意文件或文件夹上右键，即可看到「编辑文件备注」选项。",
                "注册成功", MessageBoxButton.OK, MessageBoxImage.Information);

            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"注册失败：{ex.Message}\n\n你也可以稍后从主界面的工具栏进行注册。",
                "注册失败", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }
    }

    private void SkipLink_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
