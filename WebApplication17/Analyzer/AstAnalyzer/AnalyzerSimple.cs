using WebApplication17.Analyzer._AnalyzerUtils;
using WebApplication17.Analyzer.AstBuilder;

namespace WebApplication17.Analyzer.AstAnalyzer;

public interface IAnalyzer
{
    public CodeAnalysisResult AnalyzeUserCode();
    
}

public class AnalyzerSimple : IAnalyzer
{
    private readonly ILexer _lexerSimple;
    private readonly IParser _parserSimple;
    private readonly AstNodeProgram _userProgramRoot;
    private readonly AstNodeProgram? _templateProgramRoot;
    
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
                Type = new ArrayType { BaseType = MemberType.String, Dim = 1 },
                Identifier = new Token(TokenType.Ident, 0, "args")
            }
        ],
    };

    public AnalyzerSimple(string fileContents)
    {
        _lexerSimple = new LexerSimple();
        _parserSimple = new ParserSimple();
        
        _userProgramRoot = _parserSimple.ParseProgram(_lexerSimple.Tokenize(fileContents));
    }
    
    public AnalyzerSimple(string fileContents, string templateContents)
    {
        _lexerSimple = new LexerSimple();
        _parserSimple = new ParserSimple();

        _userProgramRoot = _parserSimple.ParseProgram(_lexerSimple.Tokenize(fileContents));
        _templateProgramRoot = _parserSimple.ParseProgram(_lexerSimple.Tokenize(templateContents));
    }

    public CodeAnalysisResult AnalyzeUserCode()
    {
        AstNodeClassMemberFunc? main = FindMainFunction();
        
        MainMethod? mainMethod = MainMethod.MakeFromAstNodeMain(main);
        string className = GetClassName();
        bool validatedTemplateFunctions = _templateProgramRoot == null ||  ValidateTemplateFunctions();
        
        return new CodeAnalysisResult(mainMethod, className, validatedTemplateFunctions);
    }
    
    private AstNodeClassMemberFunc? FindMainFunction()
    {
        foreach (var nodeClass in _userProgramRoot.ProgramClasses)
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

    private bool ValidateTemplateFunctions()
    {
        bool foundMatch = false;
        foreach (var currClass in _templateProgramRoot.ProgramClasses)
        {
            foreach (var classMember in currClass.ClassScope.ClassMembers)
            {
                classMember.ClassMember.Switch(
                    t0 => foundMatch = FindAndCompareFunc(t0, _userProgramRoot) != null,
                    _ => { }
                );
                if (foundMatch)
                {
                    return true;
                }
            }
        }

        return foundMatch;
    }

    private AstNodeClassMemberFunc? FindAndCompareFunc(AstNodeClassMemberFunc baselineFunc, AstNodeProgram toBeSearched)
    {
        foreach (var programClass in toBeSearched.ProgramClasses)
        {
            List<AstNodeClassMemberFunc> matchedFunctions = programClass.ClassScope.ClassMembers
                .Where(func => func.ClassMember.IsT0 && func.ClassMember.AsT0.Identifier.Value.Equals(baselineFunc.Identifier.Value))
                .Select(func => func.ClassMember.AsT0)
                .ToList();
            foreach (var matchedFunction in matchedFunctions)
            {
                if (ValidateFunctionSignature(baselineFunc, matchedFunction))
                {
                    return matchedFunction;
                }
            }
        }

        return null;
    }

    public string GetClassName()
    {
        return _userProgramRoot.ProgramClasses[0].Identifier.Value; //currently presumes only one class per file, naive but this is a simple parser not without cause
    }
    
    private bool ValidateFunctionSignature(AstNodeClassMemberFunc baseline, AstNodeClassMemberFunc compared)
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