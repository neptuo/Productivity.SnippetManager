using Neptuo.Text.Tokens;

namespace Neptuo.Productivity.SnippetManager.Variables;

public class TokenSnippetVariableScanner : ISnippetVariableScanner
{
    public IReadOnlyList<VariableReference> Scan(string text)
    {
        var parser = CreateParser();
        var names = new HashSet<string>();
        var references = new List<VariableReference>();

        parser.OnParsedToken += (sender, e) =>
        {
            if (names.Add(e.Token.Fullname))
                references.Add(new VariableReference(e.Token.Fullname));
        };

        if (!parser.Parse(text))
            return Array.Empty<VariableReference>();

        return references;
    }

    private static TokenParser CreateParser()
    {
        var parser = new TokenParser();
        parser.Configuration.AllowTextContent = true;
        parser.Configuration.AllowMultipleTokens = true;
        parser.Configuration.AllowEscapeSequence = true;
        parser.Configuration.AllowAttributes = false;
        parser.Configuration.AllowDefaultAttributes = false;
        return parser;
    }
}
