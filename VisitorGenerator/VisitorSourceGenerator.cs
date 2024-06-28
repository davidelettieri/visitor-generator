using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace VisitorGenerator
{
    public class Walker : CSharpSyntaxWalker
    {
        public List<ClassDeclarationSyntax> ImplementingTypes { get; } = new List<ClassDeclarationSyntax>();

        public Walker(string interfaceName)
        {
            InterfaceName = interfaceName;
        }

        private string InterfaceName { get; }

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
        private const string AttributeText = """
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

        private static void IndentCurrentLineIfRequired(bool indent, StringBuilder nodeSB)
        {
            if (indent)
            {
                nodeSB.Append("    ");
            }
        }

        /// <summary>
        /// Created on demand before each generation pass
        /// </summary>
        class SyntaxReceiver : ISyntaxReceiver
        {
            public List<InterfaceDeclarationSyntax> Interfaces { get; } = new List<InterfaceDeclarationSyntax>();

            /// <summary>
            /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
            /// </summary>
            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                // any field with at least one attribute is a candidate for property generation
                if (syntaxNode is InterfaceDeclarationSyntax decl && decl.AttributeLists.Count > 0)
                {
                    var added = false;
                    foreach (var attributeList in decl.AttributeLists)
                    {
                        foreach (var attribute in attributeList.Attributes)
                        {
                            if (attribute.ToString() == "VisitorNode")
                            {
                                Interfaces.Add(decl);
                                added = true;
                                break;
                            }
                        }

                        if (added)
                        {
                            break;
                        }
                    }
                }
            }
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<InterfaceDeclarationSyntax?> classDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                    transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
                .Where(static m => m is not null);

            IncrementalValueProvider<(Compilation Left, ImmutableArray<InterfaceDeclarationSyntax?> Right)>
                compilationAndClasses =
                    context.CompilationProvider.Combine(classDeclarations.Collect());

            context.RegisterPostInitializationOutput(i => i.AddSource("VisitorNodeAttribute.g.cs", AttributeText));
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
            => syntaxNode is InterfaceDeclarationSyntax { AttributeLists.Count: > 0 } ids;

        static InterfaceDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
        {
            var interfaceDecl = context.Node as InterfaceDeclarationSyntax;

            if (interfaceDecl is null)
            {
                return null;
            }

            foreach (AttributeListSyntax attributeListSyntax in interfaceDecl.AttributeLists)
            {
                foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                {
                    IMethodSymbol? attributeSymbol =
                        context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol as IMethodSymbol;
                    if (attributeSymbol == null)
                    {
                        continue;
                    }

                    INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                    string fullName = attributeContainingTypeSymbol.ToDisplayString();

                    if (fullName == "VisitorGenerator.VisitorNodeAttribute")
                    {
                        return interfaceDecl;
                    }
                }
            }

            return null;
        }
    }
}