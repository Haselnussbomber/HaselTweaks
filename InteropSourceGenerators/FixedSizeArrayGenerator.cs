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
internal sealed class FixedSizeArrayGenerator : IIncrementalGenerator
{
    private const string AttributeName = "FFXIVClientStructs.Interop.Attributes.FixedSizeArrayAttribute`1";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<(Validation<DiagnosticInfo, StructInfo> StructInfo,
            Validation<DiagnosticInfo, FixedSizeArrayInfo> FixedSizeArrayInfo)> structAndFixedSizeArrayInfos =
            context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    AttributeName,
                    static (node, _) => node is VariableDeclaratorSyntax
                    {
                        Parent: VariableDeclarationSyntax
                        {
                            Parent: FieldDeclarationSyntax
                            {
                                Parent: StructDeclarationSyntax, AttributeLists.Count: > 0
                            }
                        }
                    },
                    static (context, _) =>
                    {
                        var structSyntax = (StructDeclarationSyntax)context.TargetNode.Parent!.Parent!.Parent!;

                        var fieldSymbol = (IFieldSymbol)context.TargetSymbol;

                        return (Struct: StructInfo.GetFromSyntax(structSyntax),
                            Info: FixedSizeArrayInfo.GetFromRoslyn(fieldSymbol));
                    });

        // group by struct
        IncrementalValuesProvider<(Validation<DiagnosticInfo, StructInfo> StructInfo,
            Validation<DiagnosticInfo, Seq<FixedSizeArrayInfo>> FixedSizeArrayInfos)> groupedStructInfoWithFixedSizeArrayInfos =
            structAndFixedSizeArrayInfos.TupleGroupByValidation();

        // make sure caching is working
        var structWithFixedInfos =
            groupedStructInfoWithFixedSizeArrayInfos.Select(static (item, _) =>
                (item.StructInfo, item.FixedSizeArrayInfos).Apply(static (si, fsai) =>
                    new StructWithFixedArrayInfos(si, fsai))
            );

        context.RegisterSourceOutput(structWithFixedInfos, (sourceContext, item) =>
        {
            item.Match(
                Fail: diagnosticInfos =>
                {
                    diagnosticInfos.Iter(dInfo => sourceContext.ReportDiagnostic(dInfo.ToDiagnostic()));
                },
                Succ: structWithFixedInfo =>
                {
                    sourceContext.AddSource(structWithFixedInfo.GetFileName(), structWithFixedInfo.RenderSource());
                });
        });
    }

    internal sealed record FixedSizeArrayInfo(string FieldName, string TypeName, int Count)
    {
        public static Validation<DiagnosticInfo, FixedSizeArrayInfo> GetFromRoslyn(IFieldSymbol fieldSymbol)
        {
            var validSymbol =
                (fieldSymbol.IsFixedSizeBuffer
                    ? Success<DiagnosticInfo, IFieldSymbol>(fieldSymbol)
                    : Fail<DiagnosticInfo, IFieldSymbol>(
                        DiagnosticInfo.Create(FixedSizedAttributeOnInvalidField,
                            fieldSymbol)))
                .Bind(symbol =>
                {
                    var pointerType = (symbol.Type as IPointerTypeSymbol)!; // we know its a pointer
                    var pointedToType = pointerType.PointedAtType;
                    return pointedToType.SpecialType != SpecialType.System_Byte
                        ? Fail<DiagnosticInfo, IFieldSymbol>(
                            DiagnosticInfo.Create(FixedSizedAttributeOnInvalidField,
                                fieldSymbol))
                        : Success<DiagnosticInfo, IFieldSymbol>(fieldSymbol);
                });
            var attribute =
                fieldSymbol.GetFirstAttributeDataByTypeName(AttributeName.Replace("`1", ""));
            var validType =
                attribute
                    .Bind<string>(attrData => attrData.AttributeClass?.TypeArguments.First().GetFullyQualifiedNameWithGenerics())
                    .ToValidation(
                        DiagnosticInfo.Create(
                            AttributeGenericTypeArgumentInvalid,
                            fieldSymbol,
                            AttributeName));
            var validCount =
                attribute.GetValidAttributeArgument<int>("Count", 0, AttributeName, fieldSymbol);

            return (validSymbol, validType, validCount).Apply((symbol, type, count) =>
                new FixedSizeArrayInfo(symbol.Name, type, count));
        }

        public void RenderFixedSizeArraySpan(IndentedStringBuilder builder)
        {
            builder.AppendLine($"public Span<{TypeName}> {FieldName}Span => new(Unsafe.AsPointer(ref {FieldName}[0]), {Count});");
        }
    }

    private sealed record StructWithFixedArrayInfos(StructInfo StructInfo, Seq<FixedSizeArrayInfo> FixedSizeArrayInfos)
    {
        public string RenderSource()
        {
            IndentedStringBuilder builder = new();

            builder.AppendLine("using System.Runtime.CompilerServices;");

            StructInfo.RenderStart(builder);

            foreach (var fsai in FixedSizeArrayInfos)
                fsai.RenderFixedSizeArraySpan(builder);

            StructInfo.RenderEnd(builder);

            return builder.ToString();
        }

        public string GetFileName()
        {
            return $"{StructInfo.Namespace}.{StructInfo.Name}.FixedSizeArrays.g.cs";
        }
    }
}
