using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Productivity.SnippetManager;

public class SnippetTokenizer
{
    public static IReadOnlyList<string> Tokenize(string input)
    {
        if (String.IsNullOrEmpty(input))
            return Array.Empty<string>();

        input = input.Trim();
        if (String.IsNullOrEmpty(input))
            return Array.Empty<string>();

        List<string> result = new List<string>();
        bool inQuoted = false;
        string currentText = string.Empty;
        for (int i = 0; i < input.Length; i++)
        {
            char item = input[i];
            if (item == '"')
            {
                inQuoted = !inQuoted;

                if (currentText.Length > 0)
                    result.Add(currentText);

                currentText = string.Empty;
            }
            else if (item == ' ' && !inQuoted)
            {
                if (currentText.Length > 0)
                    result.Add(currentText);

                currentText = string.Empty;
            }
            else
            {
                currentText += item;
            }
        }

        if (currentText.Length > 0)
            result.Add(currentText);

        return result;
    }
}
