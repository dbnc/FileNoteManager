using System.Windows;
using FileNoteManager.Core.Interfaces;
using FileNoteManager.Core.Repositories.Interfaces;
using FileNoteManager.Core.Services;
using FileNoteManager.Shell;
using FileNoteManager.UI.ViewModels;
using FileNoteManager.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace FileNoteManager.UI;

/// <summary>
/// Application entry point. Configures DI container and routes startup
/// to either MainWindow (normal launch) or QuickNoteWindow (--path arg).
/// </summary>
public partial class App : Application
{
    private IServiceProvider? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _services = BuildServiceProvider();

        // Start the file watcher immediately so rename/delete events update
        // the database even when the main window has never been opened.
        StartFileWatcher(_services);

        var pathArg = ParsePathArgument(e.Args);

        if (pathArg != null)
        {
            // Launched from Shell context menu: open quick-edit popup
            var window = _services.GetRequiredService<QuickNoteWindow>();
            window.LoadPath(pathArg);
            window.Show();
        }
        else
        {
            // Prevent app from shutting down when SetupWindow closes
            // before MainWindow has been shown
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // First run: show setup dialog if shell not registered yet
            if (!ShellRegistrar.IsRegistered())
            {
                var setup = new SetupWindow();
                setup.ShowDialog();
            }

            // Normal launch: open main management window
            ShutdownMode = ShutdownMode.OnLastWindowClose;
            var window = _services.GetRequiredService<MainWindow>();
            window.Show();
        }
    }

    private static void StartFileWatcher(IServiceProvider services)
    {
        try
        {
            var watcher = services.GetRequiredService<IFileWatcherService>();
            var repo    = services.GetRequiredService<INoteRepository>();
            foreach (var note in repo.GetAll())
                watcher.RegisterPath(note.Path);
            watcher.Start();
        }
        catch { /* never crash startup */ }
    }

    private static IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        // Register all Core services (repositories, services, database)
        services.AddFileNoteManagerCore();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<QuickNoteViewModel>();

        // Windows (transient so each launch gets a fresh instance)
        services.AddTransient<MainWindow>();
        services.AddTransient<QuickNoteWindow>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Parse --path argument from command-line args array.
    /// Returns null if the argument is not present.
    /// </summary>
    private static string? ParsePathArgument(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], "--path", StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }
        return null;
    }
}

