# Productivity.SnippetManager
Text Snippets everywhere

## Configuration

Everything in configuration is optional. You open configuration from the context menu of tray icon.
The configuration is expected to be at `~/SnippetManager.json` (user home). If you use the tray icon context menu, an example configuration will be generated for you.

```json
{
  "General": {
    "HotKey": "Control+Shift+V"
  },
  "Clipboard": {
    "Enabled": true
  },
  "Guid": {
    "Enabled": true
  },
  "GitHub": {
    "UserName": "jon",
    "AccessToken": "doe",
    "ExtraRepositories": [],
    "Enabled": true
  },
  "Xml": {
    "FilePath": "C:\\Users\\marek\\SnippetManager.xml",
    "Enabled": true
  },
  "Snippets": {
    "Hello": "Hello, World!"
  }
}
```

All of the sections has one in common, they all can contain `Enabled` field. The rule is that if the section is missing, the provider is enabled/disable by their default setting. Once you declare the section (for example to set the path to XML file), the automatic value for `Enabled` is `true`, so you don't need to specify it. If on the other hand, you want to disable some provider, that is by default enabled, just add the section with `Enabled` set to `false`.

Once you have the configuration file, the app will monitor changes and prompt you to reload the app state.

### General

Enables you to define your own global hotkey for opening snippet list. Default is `Ctrl+Shift+V`.

### Clipboard

Enables a snippet containing a content of your clipboard. Enabled by default.

### Guid

Enables a snippet containing a new guid everytime the snippet list opened. Enabled by default.

### GitHub

Enables snippets for URL of every repository you have on GitHub. Organizations requiring 2FA won't be included, but you can add extra repositories using `ExtraRepositories` array.

### XML

Enables snippets you declare in XML syntax. The `FilePath` is a path to the root file. 

```xml
<?xml version="1.0" encoding="utf-8" ?>
<Snippets xmlns="http://schemas.neptuo.com/xsd/productivity/SnippetManager.xsd">
	<Snippet Title="Google" Text="https://google.com" Priority="High" />
	<Snippet Title="Long snippet">
<![CDATA[1
2
3
4]]>
  </Snippet>
  ...
</Snippets>
```

### Snippets

In addition to the XML snippets, you can declare some snippets using `Snippets` object. The probably main difference is that the XML is more readable for longer snippets and better escapes special characters in CDATA sections. Use these as you wish.
