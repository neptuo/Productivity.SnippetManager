using System.Text;
using Neptuo.Text.Tokens;

namespace Neptuo.Productivity.SnippetManager.Variables;

public class TokenSnippetTemplateCompiler : ISnippetTemplateCompiler
{
    public ISnippetTemplate Compile(string text)
    {
        var parser = CreateParser();
        var parsedTokens = new List<TokenEventArgs>();
        parser.OnParsedToken += (sender, e) => parsedTokens.Add(e);

        if (!parser.Parse(text) || parsedTokens.Count == 0)
            return new PassthroughTemplate(text);

        var names = new HashSet<string>();
        var references = new List<VariableReference>();
        foreach (var e in parsedTokens)
        {
            if (names.Add(e.Token.Fullname))
                references.Add(new VariableReference(e.Token.Fullname));
        }

        return new TokenSnippetTemplate(text, parsedTokens, references);
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

    private sealed class PassthroughTemplate(string text) : ISnippetTemplate
    {
        public IReadOnlyList<VariableReference> Variables => Array.Empty<VariableReference>();

        public string Render(IReadOnlyDictionary<string, string?> values) => text;
    }

    private sealed class TokenSnippetTemplate(
        string text,
        IReadOnlyList<TokenEventArgs> tokens,
        IReadOnlyList<VariableReference> variables) : ISnippetTemplate
    {
        public IReadOnlyList<VariableReference> Variables => variables;

        public string Render(IReadOnlyDictionary<string, string?> values)
        {
            var sb = new StringBuilder();
            int lastEnd = 0;

            foreach (var tokenEvent in tokens)
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
    }
}
