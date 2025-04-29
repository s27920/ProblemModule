using OneOf;

namespace WebApplication17.Analyzer._AnalyzerUtils;

public enum MemberType{
    Byte, Short, Int, Long, Float, Double, Char, Boolean,
}

public enum SpecialMemberType
{
    Void, String /*questionable*/
}

public enum UnaryType{
    ArrDereference, Incr, Decr
}

public enum BinaryOperator{
    Sum, Diff, Mul, Div, Mod, And, Or, Xor
}

public enum LogicalOperator{
    And, Or, Xor
}

public enum AccessModifier{
    Public, Private, Protected
}

public enum MemberModifier{ //perhaps should consider different legal modifiers for different Token types
    Static, Final
}

public struct AstNodeProgram
{
    public List<AstNodeClass> ProgramClasses { get; set; }
}

public struct AstNodeClass()
{
    public AccessModifier ClassAccessModifier {get; set;} = AccessModifier.Private;
    public AstNodeCLassScope? ClassScope {get; set;}
    public Token Identifier { get; set; }
}

public struct AstNodeCLassScope()
{
    public List<AstNodeClassMember> ClassMembers { get; set; } = new List<AstNodeClassMember>();
    public int ScopeBeginOffset { get; set; }
    public int ScopeEndOffset { get; set; }
    
}

public struct AstNodeClassMember
{
    public OneOf<AstNodeClassMemberFunc, AstNodeClassMemberVar>  ClassMember {get; set;}
}

public struct AstNodeClassMemberFunc()
{
    public AccessModifier AccessModifier {get; set;} = AccessModifier.Public;
    public List<MemberModifier> Modifiers { get; set; } = new();
    public MemberType? FuncReturnType {get; set;}
    public Token? Identifier {get; set;}
    public List<AstNodeScopeMemberVar> FuncArgs { get; set; } = new();
    public AstNodeStatementScope? FuncScope {get; set;}
    
    public int ScopeBeginOffset { get; set; }
    public int ScopeEndOffset { get; set; }
}

public struct AstNodeClassMemberVar()
{
    public AccessModifier AccessModifier {get; set;} = AccessModifier.Public;
    public AstNodeScopeMemberVar ScopeMemberVar { get; set; } = new();
}

public struct AstNodeScopeMemberVar()
{
    public List<MemberModifier>? VarModifiers {get; set;}
    public OneOf<MemberType, SpecialMemberType> Type { get; set; }
    public Token? Identifier {get; set;}
    public Token? LitValue { get; set; }
}

public struct AstNodeStatementScope()
{
    public int ScopeBeginOffset { get; set; }
    public int ScopeEndOffset { get; set; }
    public List<AstNodeStatement> ScopedStatements { get; set; } = [];
    public bool IsMainMethodScope { get; set; } 
}

public struct AstNodeExpr()
{
    public OneOf<AstNodeBinExpr, AstNodeUnaryExpr, AstNodeExprIdent>? Variant {get; set;}
}

public struct AstNodeUnaryExpr()
{
    AstNodeExpr? Operand {get; set;}
}

public struct AstNodeBinExpr(){
    AstNodeExpr? ExprLhs {get; set;}
    AstNodeExpr? ExprRhs {get; set;}
}

public struct AstNodeLit()
{
    TokenType LitToken {get; set;}
}

public struct AstNodeExprIdent()
{
    Token? Ident {get; set;}
}


public struct AstNodeStatExpr
{
    AstNodeExpr? Expr {get; set;}
}

public struct AstNodeStatement()
{
    public OneOf<AstNodeStatementScope /* questionable but allows for easier if parsing more specfically in if (x) statement; */, AstNodeStatementUnknown> Variant {get; set;}
}
public struct AstNodeStatementUnknown(Token ident) //temporary solution to make parsing scopes work for unknown tokens TODO change this
{
    public Token Ident = ident;
}
