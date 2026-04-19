using Neptuo.Productivity.SnippetManager.Services;

namespace Neptuo.Productivity.SnippetManager.Views.DesignData;

internal class InteropService : IClipboardService, ISendTextService
{
    public static readonly InteropService Instance = new InteropService();

    public void Send(string text)
    {
    }

    public void SetText(string text)
    {
    }
}
