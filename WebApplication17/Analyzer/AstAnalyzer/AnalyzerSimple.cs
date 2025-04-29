using WebApplication17.Analyzer._AnalyzerUtils;
using WebApplication17.Analyzer.AstBuilder;

namespace WebApplication17.Analyzer.AstAnalyzer;

public interface IAnalyzer
{
    public void BuildAstFromFileContents(string fileContents);
    public void BuildAstFromFilePath(string filePath);
    public AstNodeClassMemberFunc? FindMainFunction();
    public string GetClassName();
}

public class AnalyzerSimple : IAnalyzer
{
    private readonly ILexer _lexerSimple = new LexerSimple();
    private readonly IParser _parserSimple = new ParserSimple();
    private AstNodeProgram? _programRoot = null;

    public void BuildAstFromFileContents(string fileContents)
    {
        List<Token> tokens = _lexerSimple.Tokenize(fileContents);
        _programRoot = _parserSimple.ParseProgram(tokens);
    }

    public void BuildAstFromFilePath(string filePath)
    {
        List<Token> tokens = _lexerSimple.Tokenize(File.ReadAllText(filePath));
        _programRoot = _parserSimple.ParseProgram(tokens);
    }
    
    public AstNodeClassMemberFunc? FindMainFunction()
    {
        if (_programRoot == null)
        {
            throw new NullReferenceException("program must be parsed first");
        }
        
        foreach (var nodeClass in _programRoot.ProgramClasses)
        {
            foreach (AstNodeClassMember member in nodeClass.ClassScope.ClassMembers)
            {
                if (member.ClassMember.IsT0 && ValidateFunctionSignature(_baselineMainSignature, member.ClassMember.AsT0))
                {
                    return member.ClassMember.AsT0;
                }
            }
        }
        return null;
    }

    public string GetClassName()
    {
        if (_programRoot == null)
        {
            throw new NullReferenceException("program must be parsed first");
        }
        return _programRoot.ProgramClasses[0].Identifier.Value;
    }


    private AstNodeClassMemberFunc _baselineMainSignature = new()
    {
        AccessModifier = AccessModifier.Public,
        Modifiers = [MemberModifier.Static],
        FuncReturnType = SpecialMemberType.Void,
        Identifier = new Token(TokenType.Ident, 0, "main"),
        FuncArgs =
        [
            new AstNodeScopeMemberVar()
            {
                Type = new ArrayType() { BaseType = MemberType.String, Dim = 1 },
                Identifier = new Token(TokenType.Ident, 0, "args")
            }
        ],
    };
    public bool ValidateFunctionSignature(AstNodeClassMemberFunc baseline, AstNodeClassMemberFunc compared)
    {
        if (baseline.AccessModifier != compared.AccessModifier)
        {
            return false;
        }

        if (!baseline.Modifiers.OrderBy(m => m).SequenceEqual(compared.Modifiers.OrderBy(m => m)))
        {
            return false;
        }

        bool isValid = true;
        baseline.FuncReturnType?.Switch(
            t0 => isValid =  compared.FuncReturnType.Value.IsT0 && t0 == compared.FuncReturnType.Value.AsT0,
            t1 => isValid = compared.FuncReturnType.Value.IsT1 && t1 == compared.FuncReturnType.Value.AsT1,
            t2 =>
            {
                if (!compared.FuncReturnType.Value.IsT2)
                {
                    isValid = false;
                }
                var comparedArray = compared.FuncReturnType.Value.AsT2;
                isValid = t2.BaseType.IsT0 &&
                          comparedArray.BaseType.IsT0 &&
                          t2.BaseType.AsT0 == comparedArray.BaseType.AsT0 &&
                          t2.Dim == comparedArray.Dim;
            }
            
        );
        if (!isValid)
        {
            return false;
        }

        if (baseline.Identifier?.Value != compared.Identifier?.Value)
        {
            return false;
        }

        if (baseline.FuncArgs.Count != compared.FuncArgs.Count)
        {
            return false;
        }

        for (int i = 0; i < baseline.FuncArgs.Count; i++)
        {
            baseline.FuncArgs[i].Type.Switch(
                t0 => isValid = compared.FuncArgs[i].Type.IsT0 && compared.FuncArgs[i].Type.AsT0 == t0,
                t1 => {
                    if (!compared.FuncArgs[i].Type.IsT1)
                    {
                        isValid = false;
                    }
                    var comparedArray = compared.FuncArgs[i].Type.AsT1;
                    isValid = t1.BaseType.IsT0 &&
                              comparedArray.BaseType.IsT0 &&
                              t1.BaseType.AsT0 == comparedArray.BaseType.AsT0 &&
                              t1.Dim == comparedArray.Dim;
                     }
                );
            if (!isValid)
            {
                return isValid;
            }
        }
        return isValid;
    }
}