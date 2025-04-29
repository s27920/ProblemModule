using WebApplication17.Analyzer._AnalyzerUtils;
using WebApplication17.Analyzer.AstBuilder;

namespace WebApplication17.Analyzer.AstAnalyzer;

public interface IAnalyzer
{
    public AstNodeProgram BuildAstFromFileContents(string fileContents);
    public AstNodeProgram BuildAstFromFilePath(string filePath);
    public AstNodeClassMemberFunc? FindMainFunction(AstNodeProgram programRoot);
}

public class AnalyzerSimple : IAnalyzer
{
    private readonly ILexer _lexerSimple = new LexerSimple();
    private readonly IParser _parserSimple = new ParserSimple();

    public AstNodeProgram BuildAstFromFileContents(string fileContents)
    {
        List<Token> tokens = _lexerSimple.Tokenize(fileContents);
        return _parserSimple.ParseProgram(tokens);
    }

    public AstNodeProgram BuildAstFromFilePath(string filePath)
    {
        List<Token> tokens = _lexerSimple.Tokenize(File.ReadAllText(filePath));
        return _parserSimple.ParseProgram(tokens);
    }

    public AstNodeClassMemberFunc? FindMainFunction(AstNodeProgram programRoot)
    {
        AstNodeClassMemberFunc? main = null;
        foreach (AstNodeClass programClass in programRoot.ProgramClasses)
        {
            foreach (AstNodeClassMember member in programClass.ClassScope.ClassMembers) //TODO why did I make Class scope or whatever optional, dumb.
            {
                member.ClassMember.Switch(                                                    //TODO also fix this horrible loop inside loop
                    func =>
                    {
                        if (func.Identifier is not null && func.Identifier.Value.Equals("main"))
                        {
                            main = func;
                        }
                    },
                    mVar =>{}
                    );
            }
        }

        return main;
    }
}