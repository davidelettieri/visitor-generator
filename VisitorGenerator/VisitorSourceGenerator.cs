using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace VisitorGenerator
{
    public class Walker(string interfaceName) : CSharpSyntaxWalker
    {
        public List<ClassDeclarationSyntax> ImplementingTypes { get; } = [];

        private string InterfaceName { get; } = interfaceName;

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (node.BaseList?.Types.Count > 0)
            {
                foreach (var item in node.BaseList.Types)
                {
                    if (item.Type is IdentifierNameSyntax ins && ins.ToFullString().Trim() == InterfaceName)
                    {
                        ImplementingTypes.Add(node);
                    }
                }
            }
        }
    }

    [Generator(LanguageNames.CSharp)]
    public class VisitorSourceGenerator : IIncrementalGenerator
    {
        private static void AddVisitorInterface(Walker walker, StringBuilder sb, bool indentInterface,
            string interfaceName)
        {
            IndentCurrentLineIfRequired(indentInterface, sb);
            sb.Append("public interface ");
            sb.AppendLine(interfaceName);
            IndentCurrentLineIfRequired(indentInterface, sb);
            sb.AppendLine("{");
            foreach (var t in walker.ImplementingTypes)
            {
                IndentCurrentLineIfRequired(indentInterface, sb);
                sb.Append("    T Visit(");
                sb.Append(t.Identifier.ToFullString().TrimEnd());
                sb.AppendLine(" node);");
            }

            IndentCurrentLineIfRequired(indentInterface, sb);
            sb.AppendLine("}");
        }

        private static void AddVoidVisitorInterface(Walker walker, StringBuilder sb, bool indentInterface,
            string interfaceName)
        {
            IndentCurrentLineIfRequired(indentInterface, sb);
            sb.Append("public interface ");
            sb.AppendLine(interfaceName);
            IndentCurrentLineIfRequired(indentInterface, sb);
            sb.AppendLine("{");
            foreach (var t in walker.ImplementingTypes)
            {
                IndentCurrentLineIfRequired(indentInterface, sb);
                sb.Append("    void Visit(");
                sb.Append(t.Identifier.ToFullString().TrimEnd());
                sb.AppendLine(" node);");
            }

            IndentCurrentLineIfRequired(indentInterface, sb);
            sb.Append("}");
        }

        private static void IndentCurrentLineIfRequired(bool indent, StringBuilder nodeStringBuilder)
        {
            if (indent)
            {
                nodeStringBuilder.Append("    ");
            }
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<InterfaceDeclarationSyntax?> classDeclarations = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    "VisitorGenerator.VisitorNodeAttribute",
                    predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                    transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
                .Where(static m => m is not null);

            IncrementalValueProvider<(Compilation Left, ImmutableArray<InterfaceDeclarationSyntax?> Right)>
                compilationAndClasses =
                    context.CompilationProvider.Combine(classDeclarations.Collect());

            context.RegisterSourceOutput(compilationAndClasses,
                static (spc, source) => Execute(source.Left, source.Right, spc));
        }

        private static void Execute(Compilation compilation, ImmutableArray<InterfaceDeclarationSyntax?> classes,
            SourceProductionContext context)
        {
            if (classes.IsDefaultOrEmpty)
            {
                // nothing to do yet
                return;
            }

            var interfaces = classes.Distinct();

            foreach (var item in interfaces)
            {
                if (item is null)
                {
                    continue;
                }

                var interfaceName = item.Identifier.ToFullString().Trim();
                var walker = new Walker(interfaceName);

                var interfaceSemanticModel = compilation.GetSemanticModel(item.SyntaxTree);
                var interfaceSymbol = interfaceSemanticModel.GetDeclaredSymbol(item);

                if (interfaceSymbol is null)
                {
                    continue;
                }

                foreach (var syntaxTree in compilation.SyntaxTrees)
                {
                    if (context.CancellationToken.IsCancellationRequested)
                        return;
                    var root = syntaxTree.GetCompilationUnitRoot(context.CancellationToken);

                    walker.Visit(root);
                }

                if (walker.ImplementingTypes.Count == 0)
                {
                    return;
                }

                var sb = new StringBuilder();

                var visitorName = interfaceName + "Visitor<T>";
                var voidVisitorName = interfaceName + "Visitor";

                foreach (var t in walker.ImplementingTypes)
                {
                    var nodeSemanticModel = compilation.GetSemanticModel(t.SyntaxTree);
                    var nodeSymbol = nodeSemanticModel.GetDeclaredSymbol(t);

                    if (nodeSymbol is null)
                    {
                        continue;
                    }

                    var name = t.Identifier.ToFullString().Trim();
                    var nodeSb = new StringBuilder();
                    var indent = false;

                    if (!nodeSymbol.ContainingNamespace.IsGlobalNamespace)
                    {
                        indent = true;
                        nodeSb.Append("namespace ");
                        nodeSb.AppendLine(nodeSymbol.ContainingNamespace.ToString());
                        nodeSb.AppendLine("{");
                    }

                    IndentCurrentLineIfRequired(indent, nodeSb);
                    nodeSb.Append("public partial class ");
                    nodeSb.AppendLine(name);
                    IndentCurrentLineIfRequired(indent, nodeSb);
                    nodeSb.AppendLine("{");
                    nodeSb.Append("    public T Accept<T>(").Append(visitorName)
                        .AppendLine(" visitor) => visitor.Visit(this);");
                    nodeSb.Append("    public void Accept(").Append(voidVisitorName)
                        .AppendLine(" visitor) => visitor.Visit(this);");

                    IndentCurrentLineIfRequired(indent, nodeSb);
                    nodeSb.Append("}");

                    if (!nodeSymbol.ContainingNamespace.IsGlobalNamespace)
                    {
                        nodeSb.AppendLine("").Append("}");
                    }

                    context.AddSource(name + ".g.cs", nodeSb.ToString());

                    if (!nodeSymbol.ContainingNamespace.Equals(interfaceSymbol.ContainingNamespace,
                            SymbolEqualityComparer.Default))
                    {
                        sb.Append("using ").Append(nodeSymbol.ContainingNamespace).AppendLine(";");
                    }
                }

                var indentInterface = false;

                if (!interfaceSymbol.ContainingNamespace.IsGlobalNamespace)
                {
                    indentInterface = true;
                    sb.Append("namespace ");
                    sb.AppendLine(interfaceSymbol.ContainingNamespace.ToString());
                    sb.AppendLine("{");
                }

                IndentCurrentLineIfRequired(indentInterface, sb);
                sb.Append("public partial interface ").AppendLine(interfaceName);
                IndentCurrentLineIfRequired(indentInterface, sb);
                sb.AppendLine("{");
                IndentCurrentLineIfRequired(indentInterface, sb);
                sb.Append("    T Accept<T>(").Append(visitorName).AppendLine(" visitor);");
                sb.Append("    void Accept(").Append(voidVisitorName).AppendLine(" visitor);");
                IndentCurrentLineIfRequired(indentInterface, sb);
                sb.AppendLine("}");

                AddVisitorInterface(walker, sb, indentInterface, visitorName);
                AddVoidVisitorInterface(walker, sb, indentInterface, voidVisitorName);

                if (!interfaceSymbol.ContainingNamespace.IsGlobalNamespace)
                {
                    sb.AppendLine("").Append("}");
                }

                context.AddSource(interfaceName + "Visitor.g.cs", sb.ToString());
            }
        }

        private static bool IsSyntaxTargetForGeneration(SyntaxNode syntaxNode)
        {
            var result = syntaxNode is InterfaceDeclarationSyntax;

            return result;
        }

        static InterfaceDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorAttributeSyntaxContext context)
        {
            var interfaceDecl = context.TargetNode as InterfaceDeclarationSyntax;

            if (interfaceDecl is null)
            {
                return null;
            }

            return interfaceDecl;
        }
    }
}