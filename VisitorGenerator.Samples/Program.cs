using VisitorGenerator;
using VisitorGenerator.Samples.AdditionalNodes;

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        IOpResult r = new Success();
    }
}

[VisitorNode]
public partial interface IOpResult
{
}

public partial class Success : IOpResult { }
public partial class Failure : IOpResult { }
public partial class InvalidOperation : IOpResult { }
public partial class NoOperation : IOpResult { }