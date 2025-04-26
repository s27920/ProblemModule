using WebApplication17.Analyzer._AnalyzerUtils;

namespace WebApplication17.Analyzer;

public interface IParser
{
    public AstNodeProgram Parse(List<Token> tokens);
    public List<AstNodeStatementScope> GetScopes();
}


public class ParserSimple : IParser
{
    private int _currPos;
    private List<Token> _tokens;
    private List<AstNodeStatementScope> _scopes;

    public AstNodeProgram Parse(List<Token> tokens)
    {
        _currPos = 0;
        _tokens = tokens;
        _scopes = new List<AstNodeStatementScope>();
        
        AstNodeProgram program = new AstNodeProgram();
        while (PeekToken() != null)
        {
            AstNodeStatement? statement = ParseStatement();
            if (statement != null)
            {
                program.Statements.Add(statement.Value);
            }
        }

        return program;
    }

    private AstNodeStatement? ParseStatement()
    {
        AstNodeStatement? statement = null;
        Token? token = PeekToken();
        TokenType tokenType = token.Type;
        switch (tokenType)
        {
            case TokenType.Ident:
                statement = ParseIdent(); //super simplified for now, this is meant to find main. Realistically should be parse function for case some generic type token but that's more complex, if I have the time I'll do that.
                break;
            case TokenType.OpenCurly:
                statement = ParseScopeWrapper();
                break;
            case TokenType.CloseCurly:
                break;
            default:
                ConsumeToken(); //might give us trouble later
                break;
        }

        return statement;
    }

    private AstNodeStatement ParseScopeWrapper()
    {
        return new AstNodeStatement()
        {
            Variant = ParseScope()
        };
    }
    private AstNodeStatementScope ParseScope()
    {
        AstNodeStatementScope scope = new()
        {
            ScopeBeginOffset = ConsumeToken().FilePos //consume '{' token
        };

        AstNodeStatement? scopedStatement;
        while (PeekToken() != null && (scopedStatement = ParseStatement()) != null)
        {
            scope.ScopedStatements.Add(scopedStatement.Value);
        }

        
        var consumedToken = ConsumeToken();
        
        scope.ScopeEndOffset = consumedToken.FilePos; //consume '}' token

        _scopes.Add(scope);

        return scope;
    }

    private AstNodeStatement ParseIdent()
    {
        var consumeToken = ConsumeToken();
        if (consumeToken.Value is "main")
        {
            ConsumeToken();
            ConsumeToken();
            var scope = ParseScope();
            scope.IsMainMethodScope = true;
            return new AstNodeStatement()
            {
                Variant = scope
            };

        }
        return new AstNodeStatement()
        {
            Variant = new AstNodeStatementUnknown()
            {
                Ident = consumeToken
            }
        };
    }

    public List<AstNodeStatementScope> GetScopes()
    {
        return _scopes;
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
}