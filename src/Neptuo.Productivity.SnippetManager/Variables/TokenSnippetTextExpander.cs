using System.Text;
using Neptuo.Text.Tokens;

namespace Neptuo.Productivity.SnippetManager.Variables;

public class TokenSnippetTextExpander : ISnippetTextExpander
{
    public string Expand(string text, IReadOnlyDictionary<string, string?> values)
    {
        var parser = CreateParser();
        var parsedTokens = new List<TokenEventArgs>();
        parser.OnParsedToken += (sender, e) => parsedTokens.Add(e);

        if (!parser.Parse(text) || parsedTokens.Count == 0)
            return text;

        var sb = new StringBuilder();
        int lastEnd = 0;

        foreach (var tokenEvent in parsedTokens)
        {
            sb.Append(text, lastEnd, tokenEvent.StartPosition - lastEnd);

            if (values.TryGetValue(tokenEvent.Token.Fullname, out var value) && value != null)
                sb.Append(value);
            else
                sb.Append(text, tokenEvent.StartPosition, tokenEvent.EndPosition - tokenEvent.StartPosition);

            lastEnd = tokenEvent.EndPosition;
        }

        sb.Append(text, lastEnd, text.Length - lastEnd);

        return sb.ToString();
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
