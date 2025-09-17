namespace Neptuo.Productivity.SnippetManager.Tests;

public class SearchTokenizerTests
{
    public static IEnumerable<object[]> GetBasicData() => new object[][]
    {
        new object[] { "Hello, World!", new string[] { "Hello,", "World!" } },
        new object[] { "Hello, \"This is Sparta\" World!", new string[] { "Hello,", "This is Sparta", "World!" } }
    };

    [Theory]
    [MemberData(nameof(GetBasicData))]
    public void Basic(string input, string[] output)
    {
        var result = SearchTokenizer.Tokenize(input);
        Assert.Equal(output.Length, result.Count);

        for (int i = 0; i < output.Length; i++)
            Assert.Equal(output[i], result[i]);
    }
}
