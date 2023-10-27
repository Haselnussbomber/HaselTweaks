using System.Collections.Immutable;
using HaselTweaks.InteropGenerator;
using HaselTweaks.InteropSourceGenerators.Extensions;
using HaselTweaks.InteropSourceGenerators.Models;
using LanguageExt;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static HaselTweaks.InteropSourceGenerators.DiagnosticDescriptors;
using static LanguageExt.Prelude;

namespace HaselTweaks.InteropSourceGenerators;

[Generator]
internal sealed class AddressHookGenerator : IIncrementalGenerator
{
    private const string AddressHookAttributeName = "HaselTweaks.AddressHookAttribute`1";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<(Validation<DiagnosticInfo, ClassInfo> ClassInfo,
            Validation<DiagnosticInfo, AddressHookInfoGeneric> AddressHookInfo)> classAndAddressHookInfosGeneric =
            context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    AddressHookAttributeName,
                    static (node, _) => node is MethodDeclarationSyntax
                    {
                        Parent: ClassDeclarationSyntax,
                        AttributeLists.Count: > 0
                    },
                    static (context, _) =>
                    {
                        var classSyntax = (ClassDeclarationSyntax)context.TargetNode.Parent!;

                        var methodSyntax = (MethodDeclarationSyntax)context.TargetNode;
                        var methodSymbol = (IMethodSymbol)context.TargetSymbol;

                        return (Class: ClassInfo.GetFromSyntax(classSyntax),
                            Info: AddressHookInfoGeneric.GetFromRoslyn(methodSyntax, methodSymbol));
                    });

        // group by class
        IncrementalValuesProvider<(Validation<DiagnosticInfo, ClassInfo> ClassInfo,
            Validation<DiagnosticInfo, Seq<AddressHookInfoGeneric>> AddressHookInfos)> groupedClassInfoWithMemberInfosGeneric =
            classAndAddressHookInfosGeneric.TupleGroupByValidation();

        // make sure caching is working
        var classWithMemberInfosGeneric =
            groupedClassInfoWithMemberInfosGeneric.Select(static (item, _) =>
                (item.ClassInfo, item.AddressHookInfos).Apply(static (si, mfi) =>
                    new ClassWithAddressHookInfosGeneric(si, mfi))
            );

        context.RegisterSourceOutput(classWithMemberInfosGeneric, (sourceContext, item) =>
        {
            item.Match(
                Fail: diagnosticInfos =>
                {
                    diagnosticInfos.Iter(dInfo => sourceContext.ReportDiagnostic(dInfo.ToDiagnostic()));
                },
                Succ: classWithMemberInfoGeneric =>
                {
                    sourceContext.AddSource(classWithMemberInfoGeneric.GetFileName(), classWithMemberInfoGeneric.RenderSource());
                });
        });
    }

    internal sealed record AddressHookInfoGeneric(MethodInfo MethodInfo, string structName, string addressName)
    {
        public static Validation<DiagnosticInfo, AddressHookInfoGeneric> GetFromRoslyn(
            MethodDeclarationSyntax methodSyntax, IMethodSymbol methodSymbol)
        {
            var validSyntax = Success<DiagnosticInfo, MethodDeclarationSyntax>(methodSyntax);
            var validSymbol = Success<DiagnosticInfo, IMethodSymbol>(methodSymbol);
            var paramInfos =
                methodSymbol.Parameters.Select(ParameterInfo.GetFromSymbol).Sequence();

            var validMethodInfo = (validSyntax, validSymbol, paramInfos).Apply(static (syntax, symbol, pInfos) =>
                new MethodInfo(
                    symbol.Name,
                    syntax.Modifiers.ToString(),
                    symbol.ReturnType.GetFullyQualifiedNameWithGenerics(),
                    symbol.IsStatic,
                    pInfos.ToImmutableArray()
                ));

            var hookAttr = methodSymbol.GetFirstAttributeDataByTypeName(AddressHookAttributeName.Replace("`1", ""));

            var validStructName = hookAttr
                .Select(attributeData => attributeData.AttributeClass?.TypeArguments[0].GetFullyQualifiedNameWithGenerics())
                .ToValidation(DiagnosticInfo.Create(AttributeGenericTypeArgumentInvalid, methodSymbol, AddressHookAttributeName.Replace("`1", "")));

            var validAddressName = hookAttr
                .GetValidAttributeArgument<string>("AddressName", 0, AddressHookAttributeName.Replace("`1", ""), methodSymbol);

            return (validMethodInfo, validStructName, validAddressName).Apply((methodInfo, structName, addressName) =>
                new AddressHookInfoGeneric(methodInfo, structName ?? "", addressName));
        }

        public void RenderDelegate(IndentedStringBuilder builder)
        {
            builder.AppendLine($"private delegate {MethodInfo.ReturnType} {MethodInfo.Name}Delegate({MethodInfo.GetParameterTypesAndNamesString()});");
        }

        public void RenderHook(IndentedStringBuilder builder)
        {
            builder.AppendLine($"private Dalamud.Hooking.Hook<{MethodInfo.Name}Delegate> {MethodInfo.Name}Hook {{ get; set; }} = null!;");
        }

        public void RenderSetupHook(IndentedStringBuilder builder)
        {
            builder.AppendLine($"{MethodInfo.Name}Hook = HaselCommon.Service.GameInteropProvider.HookFromAddress<{MethodInfo.Name}Delegate>((nint){structName}.Addresses.{addressName}.Value, {MethodInfo.Name});");
        }
    }

    private sealed record ClassWithAddressHookInfosGeneric(ClassInfo ClassInfo,
        Seq<AddressHookInfoGeneric> AddressHookInfos)
    {
        public string RenderSource()
        {
            IndentedStringBuilder builder = new();

            ClassInfo.RenderStart(builder);

            AddressHookInfos.Iter(mfi => mfi.RenderDelegate(builder));
            AddressHookInfos.Iter(mfi => mfi.RenderHook(builder));

            builder.AppendLine("");
            builder.AppendLine("public override void SetupAddressHooks()");
            builder.AppendLine("{");
            builder.Indent();
            AddressHookInfos.Iter(mfi => mfi.RenderSetupHook(builder));
            builder.DecrementIndent();
            builder.AppendLine("}");

            ClassInfo.RenderEnd(builder);

            return builder.ToString();
        }

        public string GetFileName()
        {
            return $"{ClassInfo.Namespace}.{ClassInfo.Name}.AddressHooksGeneric.g.cs";
        }
    }
}
