namespace WebApplication17.Executor;

public class TestCase(string testInput, string expectedOutput)
{
    public string TestInput => testInput;

    public string ExpectedOutput => expectedOutput;
}