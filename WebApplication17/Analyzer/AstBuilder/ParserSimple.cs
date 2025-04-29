using OneOf;
using WebApplication17.Analyzer._AnalyzerUtils;

namespace WebApplication17.Analyzer.AstBuilder;

public interface IParser
{
    public AstNodeProgram ParseProgram(List<Token> tokens);
}

public class ParserSimple : IParser
{
    private int _currPos;
    private List<Token> _tokens;

    public AstNodeProgram ParseProgram(List<Token> tokens)
    {
        _tokens = tokens;
        _currPos = 0;
        AstNodeProgram program = new();
        while (PeekToken() is not null)
        {
            program.ProgramClasses.Add(ParseClass());   
        }

        return program;
    }

    private AstNodeClass ParseClass()
    {
        AccessModifier? accessModifier = TokenIsAccessModifier(PeekToken());
        AstNodeClass nodeClass = new();
        if (accessModifier != null)
        {
            nodeClass.ClassAccessModifier = accessModifier.Value;
            ConsumeToken();
        }
        //TODO add parsing for class modifiers like static, final etc.
        
        ConsumeIfOfType(TokenType.Class, "class");
        nodeClass.Identifier = ConsumeIfOfType(TokenType.Ident, "class name"); //again no, but thanks rider
        nodeClass.ClassScope = ParseClassScope();


        return nodeClass;
        
    }

    private AstNodeCLassScope? ParseClassScope()
    {

        AstNodeCLassScope classScope = new()
        {
            ScopeBeginOffset = ConsumeIfOfType(TokenType.OpenCurly, "'{'").FilePos
        };

        
        while (!CheckTokenType(TokenType.CloseCurly))
        {
            classScope.ClassMembers.Add(ParseClassMember());
        }
        
        classScope.ScopeEndOffset = ConsumeIfOfType(TokenType.CloseCurly, "'}'").FilePos;
        return classScope;
    }

    private AstNodeClassMember ParseClassMember()
    {

        int forwardOffset = 0;
        while (!CheckTokenType(TokenType.Ident, forwardOffset))
        {
            forwardOffset++;
        }

        AstNodeClassMember classMember = new();
        if (CheckTokenType(TokenType.Assign, forwardOffset+1) || CheckTokenType(TokenType.Semi, forwardOffset+1)) //variable declaration
        {
            classMember.ClassMember = ParseMemberVariableDeclaration();
        }
        else if (CheckTokenType(TokenType.OpenParen, forwardOffset+1)) //function declaration
        {

            classMember.ClassMember = ParseMemberFunctionDeclaration();
        }

        return classMember;
    }
    
    private AstNodeClassMemberFunc ParseMemberFunctionDeclaration()
    {
        AstNodeClassMemberFunc memberFunc = new();
        AccessModifier? accessModifier = TokenIsAccessModifier(PeekToken());
        if (accessModifier is not null)
        {
            memberFunc.AccessModifier = accessModifier.Value;
            ConsumeToken();
        }

        memberFunc.Modifiers = ParseModifiers();


        OneOf<MemberType,SpecialMemberType, ArrayType>? type = TokenIsType(PeekToken());

        if (type == null)
        {
            throw new JavaSyntaxException("return type required");
        }

        ConsumeToken();
        memberFunc.FuncReturnType = type.Value;

        memberFunc.Identifier = ConsumeIfOfType(TokenType.Ident, "identifier");

        ConsumeIfOfType(TokenType.OpenParen, "'('");
        List<AstNodeScopeMemberVar> funcArguments = new();

        while (!CheckTokenType(TokenType.CloseParen))
        {
            funcArguments.Add(ParseScopeMemberVariableDeclaration([MemberModifier.Final]));
        }

        memberFunc.FuncArgs = funcArguments;

        ConsumeIfOfType(TokenType.CloseParen, "')'");

        memberFunc.FuncScope = ParseStatementScope();
        return memberFunc;
    }

    private List<MemberModifier> ParseModifiers()
    {
        List<MemberModifier> modifiers = new();
        while (TokenIsType(PeekToken()) == null)
        {

            MemberModifier? modifier;
            if ((modifier = TokenIsModifier(PeekToken())) != null)
            {
                modifiers.Add(modifier.Value);
                ConsumeToken();
            }
        }

        return modifiers;
    }

    private AstNodeScopeMemberVar ParseScopeMemberVariableDeclaration(MemberModifier[] permittedModifiers)
    {
        AstNodeScopeMemberVar scopedVar = new();
        List<MemberModifier> modifiers = ParseModifiers();

        foreach (MemberModifier modifier in modifiers)
        {
            if (!permittedModifiers.Contains(modifier))
            {
                throw new JavaSyntaxException("Illegal modifier");
            }
        }

        MemberType? memberType = TokenIsSimpleType(PeekToken());
        ConsumeToken();
        
        if (memberType is null)
        {
            throw new JavaSyntaxException("type expected");
        }
        
        int dim = 0;
        if (CheckTokenType(TokenType.OpenBrace) && CheckTokenType(TokenType.CloseBrace, 1)) //TODO abstract this
        {
            ConsumeToken();
            ConsumeToken();
            dim++;
            while (CheckTokenType(TokenType.OpenBrace) && CheckTokenType(TokenType.CloseBrace, 1))
            {
                dim++;
                ConsumeToken();
                ConsumeToken();
            }
            scopedVar.Type = new ArrayType()
            {
                BaseType = memberType.Value,
                Dim = dim
            };
        }
        else
        {
            scopedVar.Type = memberType.Value;
        }
        
        scopedVar.VarModifiers = modifiers;
        scopedVar.Identifier = ConsumeIfOfType(TokenType.Ident, "ident");
        //TODO important add literal value parsing to allow for actual declarations, left blank for now
        return scopedVar;
    }

    private AstNodeClassMemberVar ParseMemberVariableDeclaration()
    {
        AstNodeClassMemberVar memberVar = new();
        AccessModifier? accessModifier = TokenIsAccessModifier(PeekToken());
        memberVar.AccessModifier = accessModifier ?? AccessModifier.Public;
        memberVar.ScopeMemberVar = ParseScopeMemberVariableDeclaration([MemberModifier.Final, MemberModifier.Static]);
        return memberVar;
    }

    private AstNodeStatement ParseDefaultStat()
    {
        return new AstNodeStatement()
        {
            Variant = new AstNodeStatementUnknown(ConsumeToken())
        };
    }
    
    private AstNodeStatement? ParseStatement()
    {
        switch (PeekToken().Type)
        {
            case TokenType.OpenCurly:
                return ParseScopeWrapper();
            case TokenType.CloseCurly:
                return null;
            default:
                return ParseDefaultStat();
        }
    }

    private AstNodeStatement ParseScopeWrapper()
    {
        return new AstNodeStatement()
        {
            Variant = ParseStatementScope()
        };
    }
    private AstNodeStatementScope ParseStatementScope()
    {
        AstNodeStatementScope scope = new()
        {
            ScopeBeginOffset = ConsumeIfOfType(TokenType.OpenCurly, "'{'").FilePos //consume '{' token
        };

        AstNodeStatement? scopedStatement;
        while (PeekToken() != null && (scopedStatement = ParseStatement()) != null)
        {
            scope.ScopedStatements.Add(scopedStatement);
        }
        
        scope.ScopeEndOffset = ConsumeIfOfType(TokenType.CloseCurly, "'}'").FilePos; //consume '}' token
        
        return scope;
    }
    
    private Token ConsumeToken()
    {
        if (_currPos >= _tokens.Count)
        {
            throw new InvalidOperationException("No more tokens");
        }

        return _tokens[_currPos++];
    }
    
    private Token? PeekToken(int offset = 0)
    {
        int accessIndex = _currPos + offset;
        if (accessIndex < _tokens.Count) 
        {
            return _tokens[accessIndex];
        }

        return null;
    }

    private bool CheckTokenType(TokenType tokenType, int offset = 0)
    {
        Token? peekedToken = PeekToken(offset);
        if (peekedToken is not null && peekedToken.Type == tokenType)
        {
            return true;
        }

        return false;
    }
    
    private Token ConsumeIfOfType(TokenType tokenType, string expectedTokenMsg)
    {
        Token? peekedToken = PeekToken();
        if (peekedToken != null && peekedToken.Type == tokenType)
        {
            return ConsumeToken();
        }
        throw new JavaSyntaxException($"Expected {expectedTokenMsg} declaration");
    }
    private AccessModifier? TokenIsAccessModifier(Token? token)
    {
        AccessModifier? result = null;
        if (token is null)
        {
            return result;
        }
        switch (token.Type)
        {
            case TokenType.Private:
                return AccessModifier.Private;
            case TokenType.Protected:
                return AccessModifier.Protected;
            case TokenType.Public:
                return AccessModifier.Public;
            default:
                return null;
        }
    }


    private MemberModifier? TokenIsModifier(Token? token)
    {
        MemberModifier? modifier = null;
        if (token is null)
        {
            return modifier;
        }

        return token.Type switch
        {
            TokenType.Final => MemberModifier.Final,
            TokenType.Static => MemberModifier.Static,
            _ => null,
        };

    }
    private OneOf<MemberType, SpecialMemberType, ArrayType>? TokenIsType(Token? token) //TODO name is not really descriptive, not to me at least, change it
    {
        if (token == null)
        {
            return null;
        }
        
        MemberType? memberType = TokenIsSimpleType(token);

        
        if (memberType is null)
        {
            if (token.Type == TokenType.Void)
            {
                return SpecialMemberType.Void;
            }

            return null;
        }
        
       
        
        return memberType;
    }

    private MemberType? TokenIsSimpleType(Token? token)
    {

        MemberType? result = null;
        if (token is null)
        {
            return result;
        }
        

        return token.Type switch
        {
            TokenType.Byte => MemberType.Byte,
            TokenType.Short => MemberType.Short,
            TokenType.Int => MemberType.Int,
            TokenType.Long => MemberType.Long,
            TokenType.Float => MemberType.Float,
            TokenType.Double => MemberType.Double,
            TokenType.Char => MemberType.Char,
            TokenType.Boolean => MemberType.Boolean,
            TokenType.String => MemberType.String,
            _ => null
        };
    }
}