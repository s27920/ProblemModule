using ConsoleApp12.Analyzer.AstBuilder;
using WebApplication17.Analyzer._AnalyzerUtils;

namespace ConsoleApp12.Analyzer.AstAnalyzer;

public interface IAnalyzer
{
    public AstNodeProgram AnalyzeFileContents(string fileContents);
    public AstNodeProgram AnalyzeFromPath(string filePath);
}

public class AnalyzerSimple : IAnalyzer
{
    private readonly ILexer _lexerSimple = new LexerSimple();
    private readonly IParser _parserSimple = new ParserSimple();

    public AstNodeProgram AnalyzeFileContents(string fileContents)
    {
        List<Token> tokens = _lexerSimple.Tokenize(fileContents);
        return _parserSimple.ParseProgram(tokens);
        
    }

    public AstNodeProgram AnalyzeFromPath(string filePath)
    {
        List<Token> tokens = _lexerSimple.Tokenize(File.ReadAllText(filePath));
        return _parserSimple.ParseProgram(tokens);
    }
}