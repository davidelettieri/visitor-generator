using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;

namespace VisitorGenerator.Tests;

public class CSharpSourceGeneratorTest<TSourceGenerator, TVerifier> : SourceGeneratorTest<TVerifier>
    where TSourceGenerator : IIncrementalGenerator, new()
    where TVerifier : IVerifier, new()
{
    private static readonly LanguageVersion DefaultLanguageVersion =
        Enum.TryParse("Default", out LanguageVersion version) ? version : LanguageVersion.CSharp6;

    protected override IEnumerable<ISourceGenerator> GetSourceGenerators()
        => new[] { new TSourceGenerator().AsSourceGenerator() };

    protected override string DefaultFileExt => "cs";

    public override string Language => LanguageNames.CSharp;

    protected override GeneratorDriver CreateGeneratorDriver(Project project, ImmutableArray<ISourceGenerator> sourceGenerators)
    {
        return CSharpGeneratorDriver.Create(
            sourceGenerators,
            project.AnalyzerOptions.AdditionalFiles,
            (CSharpParseOptions)project.ParseOptions!,
            project.AnalyzerOptions.AnalyzerConfigOptionsProvider);
    }

    protected override CompilationOptions CreateCompilationOptions()
        => new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true);

    protected override ParseOptions CreateParseOptions()
        => new CSharpParseOptions(DefaultLanguageVersion, DocumentationMode.Diagnose);
}