# visitor-generator

The source generator allows to easily implement the visitor pattern. It is a proof of concepts and it requires the usage of partial interfaces and partial classes to implements the required methods.

Starting point is an interface that all the nodes that we want to visit have to implement. For example

```csharp
[VisitorNode]
public partial interface INode
{
}
```

After this we need to define partial classes for all the nodes. For example 

```csharp
public partial class Success : INode { }
public partial class Failure : INode { }
```

With these sample the generator will produce the following code

```csharp
// Augment the base interface with the required Accept method
public partial interface INode
{
    T Accept<T>(OpResultVisitor<T> visitor);
}

// Definine the visitor interface
public interface INodeVisitor<T>
{
    T Visit(Success node);
    T Visit(Failure node);
}

// Implement the Accept<T> method on the nodes
public partial class Success
{
    public T Accept<T>(OpResultVisitor<T> visitor) => visitor.Visit(this);
}

public partial class Failure
{
    public T Accept<T>(OpResultVisitor<T> visitor) => visitor.Visit(this);
}
```

## Using the package

The package is published here on github. Please follow the documentation to add the nuget source https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-nuget-registry