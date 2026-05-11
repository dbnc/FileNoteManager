using FileNoteManager.Core.Data;
using FileNoteManager.Core.Interfaces;
using FileNoteManager.Core.Repositories.Implementation;
using FileNoteManager.Core.Repositories.Interfaces;
using FileNoteManager.Core.Services.Implementation;
using Microsoft.Extensions.DependencyInjection;

namespace FileNoteManager.Core.Services;

/// <summary>
/// Extension methods for registering FileNoteManager Core services with DI container
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Register all FileNoteManager Core services with the DI container.
    /// Call this from the application entry point (App.xaml.cs).
    /// </summary>
    /// <param name="services">The service collection to register into</param>
    /// <param name="configuration">Application configuration (optional, uses defaults if null)</param>
    public static IServiceCollection AddFileNoteManagerCore(
        this IServiceCollection services,
        FnmConfiguration? configuration = null)
    {
        configuration ??= new FnmConfiguration();

        // Configuration singleton
        services.AddSingleton(configuration);

        // Database manager singleton (shared connection pool)
        services.AddSingleton(sp =>
            new DatabaseManager(sp.GetRequiredService<FnmConfiguration>().Database.Path)
        );

        // Repositories - transient (stateless, create per request)
        services.AddTransient<INoteRepository, NoteRepository>();
        services.AddTransient<ITagRepository, TagRepository>();
        services.AddTransient<INoteTagRepository>(sp => new NoteTagRepository(
            sp.GetRequiredService<DatabaseManager>(),
            sp.GetRequiredService<ITagRepository>()
        ));
        services.AddTransient<IHistoryRepository, HistoryRepository>();
        services.AddTransient<IBackupRepository, BackupRepository>();

        // Services
        services.AddTransient<INoteService, NoteService>();
        services.AddTransient<ITagService, TagService>();
        services.AddTransient<ISearchService, SearchService>();
        services.AddTransient<IHistoryService>(sp => new HistoryService(
            sp.GetRequiredService<IHistoryRepository>(),
            sp.GetRequiredService<INoteRepository>()
        ));
        services.AddTransient<IImportExportService, ImportExportService>();

        services.AddTransient<IBackupService>(sp =>
        {
            var config = sp.GetRequiredService<FnmConfiguration>();
            return new BackupService(
                sp.GetRequiredService<IBackupRepository>(),
                config.Database.Path,
                null,
                config.Database.MaxBackupCount
            );
        });

        services.AddTransient<IStatsService>(sp =>
        {
            var config = sp.GetRequiredService<FnmConfiguration>();
            return new StatsService(
                sp.GetRequiredService<DatabaseManager>(),
                sp.GetRequiredService<INoteRepository>(),
                sp.GetRequiredService<ITagRepository>(),
                sp.GetRequiredService<IBackupRepository>(),
                config.Database.Path
            );
        });

        // FileWatcherService as singleton (long-lived watcher)
        services.AddSingleton<IFileWatcherService>(sp =>
            new FileWatcherService(sp.GetRequiredService<INoteService>())
        );

        return services;
    }
}
