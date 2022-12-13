using VisitorGenerator.Samples.AdditionalNodes;

namespace VisitorGenerator.Samples;

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        OpResult r = new Success();
        var v = new Visitor();

        var a = r.Accept(v);
    }
}

public class Visitor : OpResultVisitor<bool>
{
    public bool Visit(Success node)
    {
        return true;
    }

    public bool Visit(Failure node)
    {
        return false;
    }

    public bool Visit(InvalidOperation node)
    {
        throw new NotImplementedException();
    }

    public bool Visit(NoOperation node)
    {
        throw new NotImplementedException();
    }

    public bool Visit(Node node)
    {
        throw new NotImplementedException();
    }
}

[VisitorNode]
public partial interface OpResult
{
}

public partial class Success : OpResult { }
public partial class Failure : OpResult { }
public partial class InvalidOperation : OpResult { }
public partial class NoOperation : OpResult { }