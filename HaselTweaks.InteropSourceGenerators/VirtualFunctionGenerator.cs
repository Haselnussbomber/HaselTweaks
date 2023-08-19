using HaselTweaks.InteropGenerator;
using HaselTweaks.InteropSourceGenerators.Extensions;
using HaselTweaks.InteropSourceGenerators.Models;
using LanguageExt;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HaselTweaks.InteropSourceGenerators;

[Generator]
internal sealed class VirtualFunctionGenerator : IIncrementalGenerator
{
    private const string AttributeName = "FFXIVClientStructs.Interop.Attributes.VirtualFunctionAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<(Validation<DiagnosticInfo, StructInfo> StructInfo,
            Validation<DiagnosticInfo, VirtualFunctionInfo> VirtualFunctionInfo)> structAndVirtualFunctionInfos =
            context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    AttributeName,
                    static (node, _) => node is MethodDeclarationSyntax
                    {
                        Parent: StructDeclarationSyntax, AttributeLists.Count: > 0
                    },
                    static (context, _) =>
                    {
                        var structSyntax = (StructDeclarationSyntax)context.TargetNode.Parent!;

                        var methodSyntax = (MethodDeclarationSyntax)context.TargetNode;
                        var methodSymbol = (IMethodSymbol)context.TargetSymbol;

                        return (Struct: StructInfo.GetFromSyntax(structSyntax),
                            Info: VirtualFunctionInfo.GetFromRoslyn(methodSyntax, methodSymbol));
                    });

        // group by struct
        IncrementalValuesProvider<(Validation<DiagnosticInfo, StructInfo> StructInfo,
                Validation<DiagnosticInfo, Seq<VirtualFunctionInfo>> VirtualFunctionInfos)>
            groupedStructInfoWithVirtualInfos =
                structAndVirtualFunctionInfos.TupleGroupByValidation();

        // make sure caching is working
        var structWithVirtualInfos =
            groupedStructInfoWithVirtualInfos.Select(static (item, _) =>
                (item.StructInfo, item.VirtualFunctionInfos).Apply(static (si, vfi) =>
                    new StructWithVirtualFunctionInfos(si, vfi))
            );

        context.RegisterSourceOutput(structWithVirtualInfos, (sourceContext, item) =>
        {
            item.Match(
                Fail: diagnosticInfos =>
                {
                    diagnosticInfos.Iter(dInfo => sourceContext.ReportDiagnostic(dInfo.ToDiagnostic()));
                },
                Succ: structWithVirtualInfo =>
                {
                    sourceContext.AddSource(structWithVirtualInfo.GetFileName(), structWithVirtualInfo.RenderSource());
                });
        });
    }

    internal sealed record VirtualFunctionInfo(MethodInfo MethodInfo, uint Index)
    {
        public static Validation<DiagnosticInfo, VirtualFunctionInfo> GetFromRoslyn(
            MethodDeclarationSyntax methodSyntax, IMethodSymbol methodSymbol)
        {
            var validMethodInfo =
                MethodInfo.GetFromRoslyn(methodSyntax, methodSymbol);

            var validIndex =
                methodSymbol.GetFirstAttributeDataByTypeName(AttributeName)
                    .GetValidAttributeArgument<uint>("Index", 0, AttributeName, methodSymbol);

            return (validMethodInfo, validIndex).Apply((methodInfo, index) =>
                new VirtualFunctionInfo(methodInfo, index));
        }

        public void RenderFunctionPointer(IndentedStringBuilder builder, StructInfo structInfo)
        {
            builder.AppendLine(
                $"[FieldOffset({Index * 8})] public delegate* unmanaged[Stdcall] <{structInfo.GetThisPtrTypeString()}{MethodInfo.GetParameterTypeString()}{MethodInfo.ReturnType}> {MethodInfo.Name};");
        }

        public void RenderVirtualFunction(IndentedStringBuilder builder, string structName)
        {
            MethodInfo.RenderStart(builder);

            builder.AppendLine($"fixed({structName}* thisPtr = &this)");
            builder.AppendLine("{");
            builder.Indent();
            var paramNames = MethodInfo.GetParameterNamesString();
            if (MethodInfo.Parameters.Any())
                paramNames = ", " + paramNames;
            builder.AppendLine($"{MethodInfo.GetReturnString()}VTable->{MethodInfo.Name}(thisPtr{paramNames});");
            builder.DecrementIndent();
            builder.AppendLine("}");

            MethodInfo.RenderEnd(builder);
        }
    }

    private sealed record StructWithVirtualFunctionInfos(StructInfo StructInfo,
        Seq<VirtualFunctionInfo> VirtualFunctionInfos)
    {
        public string RenderSource()
        {
            IndentedStringBuilder builder = new();

            StructInfo.RenderStart(builder);

            builder.AppendLine("[StructLayout(LayoutKind.Explicit)]");
            builder.AppendLine($"public unsafe partial struct {StructInfo.Name}VTable");
            builder.AppendLine("{");
            builder.Indent();
            VirtualFunctionInfos.Iter(vfi => vfi.RenderFunctionPointer(builder, StructInfo));
            builder.DecrementIndent();
            builder.AppendLine("}");
            builder.AppendLine();
            builder.AppendLine($"[FieldOffset(0x0)] public {StructInfo.Name}VTable* VTable;");
            builder.AppendLine();

            foreach (var vfi in VirtualFunctionInfos)
            {
                builder.AppendLine();
                vfi.RenderVirtualFunction(builder, StructInfo.Name);
            }

            StructInfo.RenderEnd(builder);

            return builder.ToString();
        }

        public string GetFileName()
        {
            return $"{StructInfo.Namespace}.{StructInfo.Name}.VirtualFunctions.g.cs";
        }
    }
}
