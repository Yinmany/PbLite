using Microsoft.CodeAnalysis;
using System.Text;

namespace PbLite.Generator
{
    internal static class ReadEmitter
    {
        public static void Generate(StringBuilder sb, TypeInfo info, bool isValueType = false)
        {
            if (isValueType)
            {
                sb.AppendLine($"        public {info.FullyQualifiedName} Deserialize(ref ProtoReader reader, {info.FullyQualifiedName}? value)");
                sb.AppendLine("        {");
                sb.AppendLine($"            var result = value ?? default;");
            }
            else
            {
                sb.AppendLine("        public object Deserialize(ref ProtoReader reader, object? value)");
                sb.AppendLine("        {");
                sb.AppendLine($"            var result = value as {info.FullyQualifiedName} ?? new {info.FullyQualifiedName}();");
            }
            foreach (var member in info.Members)
            {
                GenerateCollectionInit(sb, member);
            }
            sb.AppendLine("            while (!reader.End)");
            sb.AppendLine("            {");
            sb.AppendLine("                reader.ReadTag(out int field, out WireType wireType);");
            sb.AppendLine("                switch (field)");
            sb.AppendLine("                {");
            foreach (var member in info.Members)
            {
                GenerateReadField(sb, member);
            }
            sb.AppendLine("                    default:");
            sb.AppendLine("                        reader.SkipField(wireType);");
            sb.AppendLine("                        break;");
            sb.AppendLine("                }");
            sb.AppendLine("            }");
            sb.AppendLine("            return result;");
            sb.AppendLine("        }");
        }

        private static void GenerateCollectionInit(StringBuilder sb, MemberInfo member)
        {
            var kind = SymbolParser.GetCollectionInfo(member.Type, out _, out _, out _);
            if (kind == CollectionKind.None) return;

            string fullName = member.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            sb.AppendLine($"            result.{member.Name} ??= new {fullName}(4);");
        }

        private static void GenerateReadField(StringBuilder sb, MemberInfo member)
        {
            int order = member.Order;
            string accessor = $"result.{member.Name}";

            var kind = SymbolParser.GetCollectionInfo(member.Type, out var elemType, out var keyType, out var valType);

            if (kind == CollectionKind.List && elemType != null)
            {
                GenerateListRead(sb, order, accessor, elemType, member.Name, member.Wire);
                return;
            }

            if (kind == CollectionKind.Map && keyType != null && valType != null)
            {
                GenerateMapRead(sb, order, accessor, keyType, valType, member.Name);
                return;
            }

            GenerateReadValue(sb, member.Type, order, accessor, "reader", member.Name, "                    ", member.Wire);
        }

        private static void GenerateListRead(StringBuilder sb, int order, string accessor,
            ITypeSymbol elemType, string memberName, PbWire wire)
        {
            string? readExpr = GetReadExpression(elemType, "reader", wire);
            string? subReadExpr = GetReadExpression(elemType, "_sub_" + memberName, wire);
            string elemFullName = elemType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            bool isPacked = readExpr != null && subReadExpr != null &&
                IsPackedScalar(elemType);

            if (isPacked)
            {
                string bytesVar = $"_bytes_{memberName}";
                int minElemSize = GetPackedElementSize(elemType, wire);
                string capExpr = minElemSize > 1
                    ? $"(int)({bytesVar}.Length / {minElemSize})"
                    : $"(int){bytesVar}.Length";
                string subVar = $"_sub_{memberName}";
                sb.AppendLine($"                    case {order}:");
                sb.AppendLine("                        if (wireType == WireType.LengthDelimited)");
                sb.AppendLine("                        {");
                sb.AppendLine($"                            var {bytesVar} = reader.ReadBytes();");
                sb.AppendLine($"                            {accessor}.EnsureCapacity({capExpr});");
                sb.AppendLine($"                            var {subVar} = new ProtoReader({bytesVar});");
                sb.AppendLine($"                            while (!{subVar}.End)");
                sb.AppendLine($"                                {accessor}.Add({subReadExpr});");
                sb.AppendLine("                        }");
                sb.AppendLine("                        else");
                sb.AppendLine($"                            {accessor}.Add({readExpr});");
                sb.AppendLine("                        break;");
            }
            else if (readExpr != null)
            {
                sb.AppendLine($"                    case {order}: {accessor}.EnsureCapacity((int)Math.Min(reader.Remaining / 8, 64)); {accessor}.Add({readExpr}); break;");
            }
            else if ((elemType.TypeKind == TypeKind.Class || elemType.TypeKind == TypeKind.Structure) && SymbolParser.HasProtoContract(elemType))
            {
                string nestedSerializer = SymbolParser.GetSerializerFullName(elemType);
                string subVar = $"_sub_{memberName}";
                sb.AppendLine($"                    case {order}:");
                sb.AppendLine("                    {");
                sb.AppendLine($"                        {accessor}.EnsureCapacity((int)Math.Min(reader.Remaining / 8, 64));");
                sb.AppendLine($"                        var {subVar} = new ProtoReader(reader.ReadBytes());");
                sb.AppendLine($"                        {accessor}.Add(({elemFullName}){nestedSerializer}.Instance.Deserialize(ref {subVar}, null));");
                sb.AppendLine("                        break;");
                sb.AppendLine("                    }");
            }
            else
            {
                sb.AppendLine($"                    // case {order}: unsupported list element type");
            }
        }

        private static void GenerateMapRead(StringBuilder sb, int order, string accessor,
            ITypeSymbol keyType, ITypeSymbol valType, string memberName)
        {
            string keyFullName = keyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string valFullName = valType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string subVar = $"_sub_{memberName}";
            string? keyExpr = GetReadExpression(keyType, subVar);
            string? valExpr = GetReadExpression(valType, subVar);

            string keyVar = $"_key_{memberName}";
            string valVar = $"_val_{memberName}";
            string fieldVar = $"_f_{memberName}";
            string wtVar = $"_wt_{memberName}";

            sb.AppendLine($"                    case {order}:");
            sb.AppendLine("                    {");
            sb.AppendLine($"                        {accessor}.EnsureCapacity((int)Math.Min(reader.Remaining / 8, 64));");
            sb.AppendLine($"                        var {subVar} = new ProtoReader(reader.ReadBytes());");
            sb.AppendLine($"                        {keyFullName} {keyVar} = default!;");
            sb.AppendLine($"                        {valFullName} {valVar} = default!;");
            sb.AppendLine($"                        while (!{subVar}.End)");
            sb.AppendLine("                        {");
            sb.AppendLine($"                            {subVar}.ReadTag(out int {fieldVar}, out WireType {wtVar});");
            sb.AppendLine($"                            switch ({fieldVar})");
            sb.AppendLine("                            {");
            sb.AppendLine($"                                case 1: {keyVar} = {keyExpr}; break;");
            sb.AppendLine($"                                case 2: {valVar} = {valExpr}; break;");
            sb.AppendLine($"                                default: {subVar}.SkipField({wtVar}); break;");
            sb.AppendLine("                            }");
            sb.AppendLine("                        }");
            sb.AppendLine($"                        {accessor}[{keyVar}] = {valVar};");
            sb.AppendLine("                        break;");
            sb.AppendLine("                    }");
        }

        private static void GenerateReadValue(StringBuilder sb, ITypeSymbol type, int order,
            string accessor, string readerVar, string memberName, string indent, PbWire wire = PbWire.Varint)
        {
            string? readExpr = GetReadExpression(type, readerVar, wire);
            string typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            if (readExpr != null)
            {
                sb.AppendLine($"{indent}case {order}: {accessor} = {readExpr}; break;");
                return;
            }

            ITypeSymbol? underlying = SymbolParser.GetNullableUnderlyingType(type);
            ITypeSymbol effectiveType = underlying ?? type;

            if ((effectiveType.TypeKind == TypeKind.Class || effectiveType.TypeKind == TypeKind.Structure) && SymbolParser.HasProtoContract(effectiveType))
            {
                string nestedSerializer = SymbolParser.GetSerializerFullName(effectiveType);
                string subVar = $"_sub_{memberName}";
                string elemTypeName = effectiveType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                sb.AppendLine($"{indent}case {order}:");
                sb.AppendLine($"{indent}{{");
                sb.AppendLine($"{indent}    var {subVar} = new ProtoReader({readerVar}.ReadBytes());");
                if (underlying != null)
                    sb.AppendLine($"{indent}    {accessor} = ({elemTypeName}){nestedSerializer}.Instance.Deserialize(ref {subVar}, null);");
                else
                    sb.AppendLine($"{indent}    {accessor} = ({typeName}){nestedSerializer}.Instance.Deserialize(ref {subVar}, {accessor});");
                sb.AppendLine($"{indent}    break;");
                sb.AppendLine($"{indent}}}");
                return;
            }

            sb.AppendLine($"{indent}// case {order}: unsupported type - skipped");
        }

        private static string? GetReadExpression(ITypeSymbol type, string readerVar, PbWire wire = PbWire.Varint)
        {
            // byte[]
            if (SymbolParser.IsByteArray(type))
            {
                return $"{readerVar}.ReadBytes().ToArray()";
            }

            // enum (including Nullable<Enum>)
            var enumType = SymbolParser.GetEnumType(type);
            if (enumType != null)
            {
                string enumFullName = enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                string readCall = wire switch
                {
                    PbWire.ZigZag  => $"{readerVar}.ReadSInt32()",
                    PbWire.Fixed32 => $"{readerVar}.ReadSFixed32()",
                    _ => $"{readerVar}.ReadInt32()",
                };
                return $"({enumFullName}){readCall}";
            }

            ITypeSymbol? underlying = SymbolParser.GetNullableUnderlyingType(type);
            SpecialType st = underlying?.SpecialType ?? type.SpecialType;

            return st switch
            {
                SpecialType.System_Int32 => wire switch
                {
                    PbWire.ZigZag  => $"{readerVar}.ReadSInt32()",
                    PbWire.Fixed32 => $"{readerVar}.ReadSFixed32()",
                    _ => $"{readerVar}.ReadInt32()",
                },
                SpecialType.System_Int64 => wire switch
                {
                    PbWire.ZigZag  => $"{readerVar}.ReadSInt64()",
                    PbWire.Fixed64 => $"{readerVar}.ReadSFixed64()",
                    _ => $"{readerVar}.ReadInt64()",
                },
                SpecialType.System_UInt32 => wire switch
                {
                    PbWire.Fixed32 => $"{readerVar}.ReadFixed32()",
                    _ => $"{readerVar}.ReadUInt32()",
                },
                SpecialType.System_UInt64 => wire switch
                {
                    PbWire.Fixed64 => $"{readerVar}.ReadFixed64()",
                    _ => $"{readerVar}.ReadUInt64()",
                },
                SpecialType.System_Single => $"{readerVar}.ReadFloat()",
                SpecialType.System_Double => $"{readerVar}.ReadDouble()",
                SpecialType.System_Boolean => $"{readerVar}.ReadBool()",
                SpecialType.System_String => $"{readerVar}.ReadString()",
                _ => null
            };
        }

        private static bool IsPackedScalar(ITypeSymbol type)
        {
            ITypeSymbol? underlying = SymbolParser.GetNullableUnderlyingType(type);
            ITypeSymbol effective = underlying ?? type;
            SpecialType st = effective.SpecialType;
            return st is SpecialType.System_Int32 or SpecialType.System_Int64
                or SpecialType.System_UInt32 or SpecialType.System_UInt64
                or SpecialType.System_Single or SpecialType.System_Double
                or SpecialType.System_Boolean
                || effective.TypeKind == TypeKind.Enum;
        }

        private static int GetPackedElementSize(ITypeSymbol type, PbWire wire)
        {
            ITypeSymbol? underlying = SymbolParser.GetNullableUnderlyingType(type);
            ITypeSymbol effective = underlying ?? type;
            SpecialType st = effective.SpecialType;

            if (effective.TypeKind == TypeKind.Enum)
                st = SpecialType.System_Int32;

            return st switch
            {
                SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Boolean
                    => wire == PbWire.Fixed32 ? 4 : 1,
                SpecialType.System_Int64 or SpecialType.System_UInt64
                    => wire == PbWire.Fixed64 ? 8 : 1,
                SpecialType.System_Single => 4,
                SpecialType.System_Double => 8,
                _ => 1
            };
        }
    }
}
