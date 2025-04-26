using OneOf;

namespace WebApplication17.Analyzer._AnalyzerUtils;

public struct AstNodeProgram()
{
    public List<AstNodeStatement> Statements = new();
}

public class AstNodeStatementScope
{
    public int ScopeBeginOffset { get; set; }
    public int ScopeEndOffset { get; set; }
    public List<AstNodeStatement> ScopedStatements { get; set; } = new();
    public bool IsMainMethodScope { get; set; } 
}


public struct
    AstNodeStatementUnknown(Token ident) //temporary solution to make parsing scopes work for unknown tokens TODO change this
{
    public Token Ident = ident;
}

public struct AstNodeStatement()
{
    public OneOf<AstNodeStatementScope, AstNodeStatementUnknown> Variant = new ();
}