﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
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

        public string InterfaceName { get; }

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

    [Generator]
    public class VisitorSourceGenerator : ISourceGenerator
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

        public void Execute(GeneratorExecutionContext context)
        {
            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
                return;

            foreach (var item in receiver.Interfaces)
            {
                var interfaceName = item.Identifier.ToFullString().Trim();
                var walker = new Walker(interfaceName);

                var interfaceSemanticModel = context.Compilation.GetSemanticModel(item.SyntaxTree);
                var interfaceSymbol = interfaceSemanticModel.GetDeclaredSymbol(item);

                foreach (var syntaxTree in context.Compilation.SyntaxTrees)
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
                    var nodeSemanticModel = context.Compilation.GetSemanticModel(t.SyntaxTree);
                    var nodeSymbol = nodeSemanticModel.GetDeclaredSymbol(t);
                    var name = t.Identifier.ToFullString().Trim();
                    var nodeSB = new StringBuilder();
                    var indent = false;

                    if (!nodeSymbol.ContainingNamespace.IsGlobalNamespace)
                    {
                        indent = true;
                        nodeSB.Append("namespace ");
                        nodeSB.AppendLine(nodeSymbol.ContainingNamespace.ToString());
                        nodeSB.AppendLine("{");
                    }

                    IndentCurrentLineIfRequired(indent, nodeSB);
                    nodeSB.Append("public partial class ");
                    nodeSB.AppendLine(name);
                    IndentCurrentLineIfRequired(indent, nodeSB);
                    nodeSB.AppendLine("{");
                    nodeSB.Append("    public T Accept<T>(").Append(visitorName)
                        .AppendLine(" visitor) => visitor.Visit(this);");
                    nodeSB.Append("    public void Accept(").Append(voidVisitorName)
                        .AppendLine(" visitor) => visitor.Visit(this);");

                    IndentCurrentLineIfRequired(indent, nodeSB);
                    nodeSB.Append("}");

                    if (!nodeSymbol.ContainingNamespace.IsGlobalNamespace)
                    {
                        nodeSB.AppendLine("").Append("}");
                    }

                    context.AddSource(name + ".g.cs", nodeSB.ToString());

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

        private static void AddVisitorInterface(Walker walker, StringBuilder sb, bool indentInterface, string interfaceName)
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
                sb.Append(t.Identifier.ToFullString());
                sb.AppendLine("node);");
            }

            IndentCurrentLineIfRequired(indentInterface, sb);
            sb.AppendLine("}");
        }

        private static void AddVoidVisitorInterface(Walker walker, StringBuilder sb, bool indentInterface, string interfaceName)
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
                sb.Append(t.Identifier.ToFullString());
                sb.AppendLine("node);");
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

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForPostInitialization((i) => i.AddSource("VisitorNodeAttribute.g.cs", AttributeText));
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
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
    }
}