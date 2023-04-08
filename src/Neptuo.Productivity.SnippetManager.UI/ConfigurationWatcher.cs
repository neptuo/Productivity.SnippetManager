using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Neptuo.Productivity.SnippetManager;

public class ConfigurationWatcher : IDisposable
{
    private readonly FileSystemWatcher? configurationWatcher;
    private readonly string configurationPath;
    private readonly Action reload;

    private CancellationTokenSource? cts;

    public ConfigurationWatcher(string configurationPath, Action reload)
    {
        this.configurationPath = configurationPath;
        this.reload = reload;
        configurationWatcher = new FileSystemWatcher(Path.GetDirectoryName(configurationPath)!, "*.json");
        configurationWatcher.Changed += OnConfigurationFileChanged;
        configurationWatcher.Deleted += OnConfigurationFileChanged;
        configurationWatcher.Created += OnConfigurationFileChanged;
        configurationWatcher.Renamed += OnConfigurationFileRenamed;
        configurationWatcher.EnableRaisingEvents = true;
    }

    private async void OnConfigurationFileRenamed(object sender, RenamedEventArgs e)
    {
        if (e.OldFullPath == configurationPath)
            await ReloadConfigurationWithConfirmationAsync();
        else
            OnConfigurationFileChanged(sender, e);
    }

    private async void OnConfigurationFileChanged(object sender, FileSystemEventArgs e)
    {
        if (e.FullPath == configurationPath)
            await ReloadConfigurationWithConfirmationAsync();
    }

    private async Task ReloadConfigurationWithConfirmationAsync()
    {
        if (cts != null)
            cts.Cancel();

        cts = new CancellationTokenSource();

        bool isCancelled = await WaitWithCancellationAsync(cts.Token);
        cts = null;

        if (isCancelled)
            return;

        reload();
    }

    private async Task<bool> WaitWithCancellationAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(2 * 1000);
        return cancellationToken.IsCancellationRequested;
    }

    public void EnableRaisingEventsFromConfigurationWatcher(bool enabled)
    {
        if (configurationWatcher != null)
            configurationWatcher.EnableRaisingEvents = enabled;
    }

    public void Dispose() 
        => configurationWatcher?.Dispose();
}
