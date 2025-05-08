// See https://aka.ms/new-console-template for more information

using VisitorGenerator;

Console.WriteLine("Hello, World!");

[VisitorNode]
public partial interface INode;

public partial class Node1 : INode;
public partial class Node2 : INode;

public class Visitor : INodeVisitor
{
    public void Visit(Node1 node1)
    {
    }

    public void Visit(Node2 node)
    {
    }
}