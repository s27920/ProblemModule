using WebApplication17.Analyzer._AnalyzerUtils;

namespace WebApplication17.Analyzer;

public interface IAnalyzer
{
    public AstNodeProgram AnalyzeFileContents(string fileContents);
    public AstNodeProgram AnalyzeFromPath(string filePath);
    public List<AstNodeStatementScope> GetScopes();
    public AstNodeStatementScope GetMainScope();
}

public class AnalyzerSimple : IAnalyzer
{
    private readonly ILexer _lexerSimple = new LexerSimple();
    private readonly IParser _parserSimple = new ParserSimple();

    public AstNodeProgram AnalyzeFileContents(string fileContents)
    {
        List<Token> tokens = _lexerSimple.Tokenize(fileContents);
        return _parserSimple.Parse(tokens);
        
    }

    public AstNodeProgram AnalyzeFromPath(string filePath)
    {
        List<Token> tokens = _lexerSimple.Tokenize(File.ReadAllText(filePath));
        return _parserSimple.Parse(tokens);
    }

    public List<AstNodeStatementScope> GetScopes()
    {
        return _parserSimple.GetScopes();
    }

    public AstNodeStatementScope GetMainScope()
    {
        return GetScopes().First(s => s.IsMainMethodScope);
    }
}