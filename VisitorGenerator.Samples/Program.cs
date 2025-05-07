using VisitorGenerator;

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
    }
}

[VisitorNode]
public partial interface IExpr
{
}

public interface IStmt
{
}

public enum TokenType
{
    
}

public record Token(TokenType Type, string Lexeme, object? Literal, int Line);


public partial class Binary(IExpr left, Token @operator, IExpr right) : IExpr
{
    public IExpr Left { get; } = left;
    public Token Operator { get; } = @operator;
    public IExpr Right { get; } = right;
}

public partial class Call(IExpr callee, Token paren, List<IExpr> arguments) : IExpr
{
    public IExpr Callee { get; } = callee;
    public Token Paren { get; } = paren;
    public List<IExpr> Arguments { get; } = arguments;
}

public partial class Get(IExpr obj, Token name) : IExpr
{
    public IExpr Obj { get; } = obj;
    public Token Name { get; } = name;
}

public partial class Grouping(IExpr expression) : IExpr
{
    public IExpr Expression { get; } = expression;
}

public partial class Literal(object value) : IExpr
{
    public object Value { get; } = value;
}

public partial class Logical(IExpr left, Token @operator, IExpr right) : IExpr
{
    public IExpr Left { get; } = left;
    public Token Operator { get; } = @operator;
    public IExpr Right { get; } = right;
}

public partial class Unary(Token @operator, IExpr right) : IExpr
{
    public Token Operator { get; } = @operator;
    public IExpr Right { get; } = right;
}

public partial class Set(IExpr obj, Token name, IExpr value) : IExpr
{
    public IExpr Obj { get; } = obj;
    public Token Name { get; } = name;
    public IExpr Value { get; } = value;
}

public partial class Super(Token keyword, Token method) : IExpr
{
    public Token Keyword { get; } = keyword;
    public Token Method { get; } = method;
}

public partial class This(Token keyword) : IExpr
{
    public Token Keyword { get; } = keyword;
}

public partial class Variable(Token name) : IExpr
{
    public Token Name { get; } = name;
}

public partial class Assign(Token name, IExpr value) : IExpr
{
    public Token Name { get; } = name;
    public IExpr Value { get; } = value;
}

public partial class AnonymousFunction(List<Token> parameters, List<IStmt> body) : IExpr
{
    public List<Token> Parameters { get; } = parameters;
    public List<IStmt> Body { get; } = body;
}