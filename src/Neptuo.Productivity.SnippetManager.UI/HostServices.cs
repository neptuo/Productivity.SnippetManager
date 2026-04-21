using Neptuo.Productivity.SnippetManager.Plugins;

namespace Neptuo.Productivity.SnippetManager;

/// <summary>
/// MEF-exported forwarder for <see cref="INavigator"/>. Exists because
/// <see cref="Navigator"/> is constructed after <see cref="PluginHost.Compose"/>
/// (it depends on composed snippet providers), while plugins need to
/// <c>[Import] INavigator</c> during composition. The host exports this proxy
/// before composing, then assigns <see cref="Target"/> once the real
/// <see cref="Navigator"/> exists. On configuration reload, <see cref="Target"/>
/// is re-pointed at the new instance.
/// </summary>
internal sealed class HostServices : INavigator
{
    public INavigator? Target { get; set; }

    public void OpenMain(bool stickToActiveCaret = true) => Target?.OpenMain(stickToActiveCaret);
    public void OpenConfiguration() => Target?.OpenConfiguration();
    public void OpenFile(string path) => Target?.OpenFile(path);
}
