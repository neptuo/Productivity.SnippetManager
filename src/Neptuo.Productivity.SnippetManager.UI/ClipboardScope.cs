using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using Windows.ApplicationModel.DataTransfer;

namespace Neptuo.Productivity.SnippetManager;

public struct ClipboardScope(DataPackage? storedContent)
{
    public static async Task<ClipboardScope> CreateAsync()
    {
        DataPackage? storedContent = null;
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

        return new ClipboardScope(storedContent);
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
                // Don't this item in history
                Clipboard.SetContentWithOptions(storedContent, new ClipboardContentOptions() { IsAllowedInHistory = false });
            }
        }
        catch
        {
            try { Clipboard.Clear(); } catch { }
        }
    }
}