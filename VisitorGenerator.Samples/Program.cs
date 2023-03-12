using VisitorGenerator;
using VisitorGenerator.Samples.AdditionalNodes;

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

public class Visitor2 : OpResultVisitor<Task>
{
    public Task Visit(Node node)
    {
        throw new NotImplementedException();
    }

    public Task Visit(Success node)
    {
        throw new NotImplementedException();
    }

    public Task Visit(Failure node)
    {
        throw new NotImplementedException();
    }

    public Task Visit(InvalidOperation node)
    {
        throw new NotImplementedException();
    }

    public Task Visit(NoOperation node)
    {
        throw new NotImplementedException();
    }
}

public class AsyncVisitor : OpResultVisitor<Task<int>>
{
    public async Task<int> Visit(Node node)
    {
        throw new NotImplementedException();
    }

    public Task<int> Visit(Success node)
    {
        throw new NotImplementedException();
    }

    public Task<int> Visit(Failure node)
    {
        throw new NotImplementedException();
    }

    public Task<int> Visit(InvalidOperation node)
    {
        throw new NotImplementedException();
    }

    public Task<int> Visit(NoOperation node)
    {
        throw new NotImplementedException();
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