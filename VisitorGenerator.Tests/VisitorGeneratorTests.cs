using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace VisitorGenerator.Tests;
using VerifyCS = CSharpSourceGeneratorVerifier<VisitorSourceGenerator>;

public class VisitorGeneratorTests
{
    [Fact]
    public async Task VisitorWithOneTestTest()
    {
        var code = """
using VisitorGenerator;

[VisitorNode]
public partial interface INode {} 

public partial class Node : INode {}
""";

        var visitorNodeAttribute = """
using System;
namespace VisitorGenerator
{
    [AttributeUsage(AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    [System.Diagnostics.Conditional("VisitorSourceGenerator_DEBUG")]
    sealed class VisitorNodeAttribute : Attribute
    {
        public VisitorNodeAttribute()
        {
        }
    }
}
""";
        
        var visitorInterfaces = """ 
public partial interface INode
{
    T Accept<T>(INodeVisitor<T> visitor);
    void Accept(INodeVisitor visitor);
}
public interface INodeVisitor<T>
{
    T Visit(Node node);
}
public interface INodeVisitor
{
    void Visit(Node node);
}
""";

        var partialNode = """
public partial class Node
{
    public T Accept<T>(INodeVisitor<T> visitor) => visitor.Visit(this);
    public void Accept(INodeVisitor visitor) => visitor.Visit(this);
}
""";
        await new VerifyCS.Test
        {
            TestState = 
            {
                Sources = { code },
                GeneratedSources =
                {
                    (typeof(VisitorSourceGenerator), "VisitorNodeAttribute.g.cs", SourceText.From(visitorNodeAttribute, Encoding.UTF8)),
                    (typeof(VisitorSourceGenerator), "Node.g.cs", SourceText.From(partialNode, Encoding.UTF8)),
                    (typeof(VisitorSourceGenerator), "INodeVisitor.g.cs", SourceText.From(visitorInterfaces, Encoding.UTF8)),
                },
            },
        }.RunAsync();
    }
}