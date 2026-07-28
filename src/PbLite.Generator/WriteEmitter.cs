using Microsoft.CodeAnalysis;
using System.Text;

namespace PbLite.Generator
{
    internal static class WriteEmitter
    {
        public static void Generate(StringBuilder sb, TypeInfo info)
        {
            sb.AppendLine($"        public int Serialize<TWriter>(TWriter writer, object value) where TWriter : IBufferWriter<byte>");
            sb.AppendLine("        {");
            sb.AppendLine($"            var v = ({info.Name})value;");
            sb.AppendLine("            int written = 0;");
            foreach (var member in info.Members)
            {
                GenerateWriteField(sb, member);
            }
            sb.AppendLine("            return written;");
            sb.AppendLine("        }");
        }

        private static void GenerateWriteField(StringBuilder sb, MemberInfo member)
        {
            int order = member.Order;
            string accessor = $"v.{member.Name}";

            var kind = SymbolParser.GetCollectionInfo(member.Type, out var elemType, out var keyType, out var valType);

            if (kind == CollectionKind.List && elemType != null)
            {
                bool isPacked = IsPackedType(elemType);

                if (isPacked)
                {
                    string lenVar = $"_len_{member.Name}";
                    string payloadVar = $"_payload_{member.Name}";
                    string itemVar = $"_item_{member.Name}";
                    sb.AppendLine($"            if ({accessor}.Count > 0)");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                written += ProtoWriter.WriteTag(writer, {order}, WireType.LengthDelimited);");
                    sb.AppendLine($"                Span<byte> {lenVar} = writer.GetSpan(5);");
                    sb.AppendLine("                writer.Advance(5);");
                    sb.AppendLine("                written += 5;");
                    sb.AppendLine($"                int {payloadVar} = 0;");
                    sb.AppendLine($"                foreach (var {itemVar} in {accessor})");
                    sb.AppendLine("                {");
                    GenerateWritePackedValue(sb, elemType, itemVar, "                    ", payloadVar);
                    sb.AppendLine("                }");
                    sb.AppendLine($"                ProtoWriter.FillFixedVarInt({lenVar}, {payloadVar});");
                    sb.AppendLine($"                written += {payloadVar};");
                    sb.AppendLine("            }");
                }
                else
                {
                    string itemVar = $"_item_{member.Name}";
                    sb.AppendLine($"            foreach (var {itemVar} in {accessor})");
                    sb.AppendLine("            {");
                    GenerateWriteValue(sb, elemType, order, itemVar, "                ", "written", skipDefault: false);
                    sb.AppendLine("            }");
                }
                return;
            }

            if (kind == CollectionKind.Map && keyType != null && valType != null)
            {
                string kvpVar = $"_kvp_{member.Name}";
                string lenVar = $"_len_{member.Name}";
                string payloadVar = $"_payload_{member.Name}";
                sb.AppendLine($"            foreach (var {kvpVar} in {accessor})");
                sb.AppendLine("            {");
                sb.AppendLine($"                written += ProtoWriter.WriteTag(writer, {order}, WireType.LengthDelimited);");
                sb.AppendLine($"                Span<byte> {lenVar} = writer.GetSpan(5);");
                sb.AppendLine("                writer.Advance(5);");
                sb.AppendLine("                written += 5;");
                sb.AppendLine($"                int {payloadVar} = 0;");
                GenerateWriteValue(sb, keyType, 1, $"{kvpVar}.Key", "                ", payloadVar, skipDefault: false);
                GenerateWriteValue(sb, valType, 2, $"{kvpVar}.Value", "                ", payloadVar, skipDefault: false);
                sb.AppendLine($"                ProtoWriter.FillFixedVarInt({lenVar}, {payloadVar});");
                sb.AppendLine($"                written += {payloadVar};");
                sb.AppendLine("            }");
                return;
            }

            GenerateWriteValue(sb, member.Type, order, accessor, "            ", "written");
        }

        private static void GenerateWriteValue(StringBuilder sb, ITypeSymbol type, int fieldNumber,
            string accessor, string indent, string counterVar, bool skipDefault = true)
        {
            ITypeSymbol? underlying = SymbolParser.GetNullableUnderlyingType(type);
            SpecialType st = underlying?.SpecialType ?? type.SpecialType;
            string typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            switch (st)
            {
                case SpecialType.System_Int32:
                    if (underlying != null)
                    {
                        sb.AppendLine($"{indent}if ({accessor}.HasValue)");
                        sb.AppendLine($"{indent}    {counterVar} += ProtoWriter.WriteInt32(writer, {fieldNumber}, {accessor}.Value);");
                    }
                    else
                        sb.AppendLine($"{indent}{counterVar} += ProtoWriter.WriteInt32(writer, {fieldNumber}, {accessor});");
                    break;
                case SpecialType.System_Int64:
                    if (underlying != null)
                    {
                        sb.AppendLine($"{indent}if ({accessor}.HasValue)");
                        sb.AppendLine($"{indent}    {counterVar} += ProtoWriter.WriteInt64(writer, {fieldNumber}, {accessor}.Value);");
                    }
                    else
                        sb.AppendLine($"{indent}{counterVar} += ProtoWriter.WriteInt64(writer, {fieldNumber}, {accessor});");
                    break;
                case SpecialType.System_UInt32:
                    if (underlying != null)
                    {
                        sb.AppendLine($"{indent}if ({accessor}.HasValue)");
                        sb.AppendLine($"{indent}    {counterVar} += ProtoWriter.WriteUInt32(writer, {fieldNumber}, {accessor}.Value);");
                    }
                    else
                        sb.AppendLine($"{indent}{counterVar} += ProtoWriter.WriteUInt32(writer, {fieldNumber}, {accessor});");
                    break;
                case SpecialType.System_UInt64:
                    if (underlying != null)
                    {
                        sb.AppendLine($"{indent}if ({accessor}.HasValue)");
                        sb.AppendLine($"{indent}    {counterVar} += ProtoWriter.WriteUInt64(writer, {fieldNumber}, {accessor}.Value);");
                    }
                    else
                        sb.AppendLine($"{indent}{counterVar} += ProtoWriter.WriteUInt64(writer, {fieldNumber}, {accessor});");
                    break;
                case SpecialType.System_Single:
                    if (underlying != null)
                    {
                        sb.AppendLine($"{indent}if ({accessor}.HasValue)");
                        sb.AppendLine($"{indent}    {counterVar} += ProtoWriter.WriteFloat(writer, {fieldNumber}, {accessor}.Value);");
                    }
                    else
                        sb.AppendLine($"{indent}{counterVar} += ProtoWriter.WriteFloat(writer, {fieldNumber}, {accessor});");
                    break;
                case SpecialType.System_Double:
                    if (underlying != null)
                    {
                        sb.AppendLine($"{indent}if ({accessor}.HasValue)");
                        sb.AppendLine($"{indent}    {counterVar} += ProtoWriter.WriteDouble(writer, {fieldNumber}, {accessor}.Value);");
                    }
                    else
                        sb.AppendLine($"{indent}{counterVar} += ProtoWriter.WriteDouble(writer, {fieldNumber}, {accessor});");
                    break;
                case SpecialType.System_Boolean:
                    if (underlying != null)
                    {
                        sb.AppendLine($"{indent}if ({accessor}.HasValue)");
                        sb.AppendLine($"{indent}    {counterVar} += ProtoWriter.WriteBool(writer, {fieldNumber}, {accessor}.Value);");
                    }
                    else
                        sb.AppendLine($"{indent}{counterVar} += ProtoWriter.WriteBool(writer, {fieldNumber}, {accessor});");
                    break;
                case SpecialType.System_String:
                    if (skipDefault)
                    {
                        sb.AppendLine($"{indent}if (!string.IsNullOrEmpty({accessor}))");
                        sb.AppendLine($"{indent}    {counterVar} += ProtoWriter.WriteString(writer, {fieldNumber}, {accessor});");
                    }
                    else
                    {
                        sb.AppendLine($"{indent}if ({accessor} != null)");
                        sb.AppendLine($"{indent}    {counterVar} += ProtoWriter.WriteString(writer, {fieldNumber}, {accessor});");
                    }
                    break;
                default:
                    if (type.TypeKind == TypeKind.Class && SymbolParser.HasProtoContract(type))
                    {
                        string nestedSerializer = type.Name + "Serializer";
                        string lenVar = $"_len_{fieldNumber}";
                        string payloadVar = $"_payload_{fieldNumber}";
                        sb.AppendLine($"{indent}if ({accessor} != null)");
                        sb.AppendLine($"{indent}{{");
                        sb.AppendLine($"{indent}    {counterVar} += ProtoWriter.WriteTag(writer, {fieldNumber}, WireType.LengthDelimited);");
                        sb.AppendLine($"{indent}    Span<byte> {lenVar} = writer.GetSpan(5);");
                        sb.AppendLine($"{indent}    writer.Advance(5);");
                        sb.AppendLine($"{indent}    {counterVar} += 5;");
                        sb.AppendLine($"{indent}    int {payloadVar} = {nestedSerializer}.Instance.Serialize(writer, {accessor});");
                        sb.AppendLine($"{indent}    ProtoWriter.FillFixedVarInt({lenVar}, {payloadVar});");
                        sb.AppendLine($"{indent}    {counterVar} += {payloadVar};");
                        sb.AppendLine($"{indent}}}");
                    }
                    else
                    {
                        sb.AppendLine($"{indent} // Unsupported type: {typeName}");
                    }
                    break;
            }
        }

        private static bool IsPackedType(ITypeSymbol type)
        {
            ITypeSymbol? underlying = SymbolParser.GetNullableUnderlyingType(type);
            SpecialType st = underlying?.SpecialType ?? type.SpecialType;
            return st is SpecialType.System_Int32 or SpecialType.System_Int64
                or SpecialType.System_UInt32 or SpecialType.System_UInt64
                or SpecialType.System_Single or SpecialType.System_Double
                or SpecialType.System_Boolean;
        }

        private static void GenerateWritePackedValue(StringBuilder sb, ITypeSymbol type,
            string accessor, string indent, string counterVar)
        {
            ITypeSymbol? underlying = SymbolParser.GetNullableUnderlyingType(type);
            SpecialType st = underlying?.SpecialType ?? type.SpecialType;

            string valueExpr = underlying != null ? $"{accessor}.Value" : accessor;

            switch (st)
            {
                case SpecialType.System_Int32:
                    sb.AppendLine($"{indent}{counterVar} += ProtoWriter.WriteVarInt64(writer, (ulong)(long){valueExpr});");
                    break;
                case SpecialType.System_Int64:
                    sb.AppendLine($"{indent}{counterVar} += ProtoWriter.WriteVarInt64(writer, (ulong)(long){valueExpr});");
                    break;
                case SpecialType.System_UInt32:
                    sb.AppendLine($"{indent}{counterVar} += ProtoWriter.WriteVarInt32(writer, (uint){valueExpr});");
                    break;
                case SpecialType.System_UInt64:
                    sb.AppendLine($"{indent}{counterVar} += ProtoWriter.WriteVarInt64(writer, (ulong){valueExpr});");
                    break;
                case SpecialType.System_Single:
                    sb.AppendLine($"{indent}{counterVar} += ProtoWriter.WriteFixed32(writer, (uint)BitConverter.SingleToInt32Bits({valueExpr}));");
                    break;
                case SpecialType.System_Double:
                    sb.AppendLine($"{indent}{counterVar} += ProtoWriter.WriteFixed64(writer, (ulong)BitConverter.DoubleToInt64Bits({valueExpr}));");
                    break;
                case SpecialType.System_Boolean:
                    sb.AppendLine($"{indent}{counterVar} += ProtoWriter.WriteVarInt32(writer, {valueExpr} ? 1u : 0u);");
                    break;
            }
        }
    }
}
