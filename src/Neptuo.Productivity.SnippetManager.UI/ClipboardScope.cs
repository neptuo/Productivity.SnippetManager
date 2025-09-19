using System.Diagnostics;
using Windows.ApplicationModel.DataTransfer;

namespace Neptuo.Productivity.SnippetManager;

public class ClipboardScope
{
    private DataPackage? storedContent;

    public static async Task<ClipboardScope> CreateAsync()
    {
        var result = new ClipboardScope();
        await result.StoreAsync();
        return result;
    }

    public async Task StoreAsync()
    {
        try
        {
            var dataPackageView = Clipboard.GetContent();
            storedContent = new DataPackage();

            // Store all available formats using the generic approach
            foreach (var format in dataPackageView.AvailableFormats)
            {
                try
                {
                    var data = await dataPackageView.GetDataAsync(format);
                    storedContent.SetData(format, data);
                }
                catch
                {
                    // Skip formats that can't be read
                    Debug.Fail($"Failed to read format '{format}'.");
                    continue;
                }
            }
        }
        catch
        {
            storedContent = null;
        }
    }

    public void Restore()
    {
        try
        {
            if (storedContent == null)
            {
                Clipboard.Clear();
            }
            else
            {
                // Don't this item in history, because the change of clipboard shouldn't actually happen
                Clipboard.SetContentWithOptions(storedContent, new ClipboardContentOptions() { IsAllowedInHistory = false });
            }
        }
        catch
        {
            try { Clipboard.Clear(); } catch { }
        }
    }
}