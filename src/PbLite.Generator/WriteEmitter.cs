using Microsoft.CodeAnalysis;
using System.Text;

namespace PbLite.Generator
{
    internal static class WriteEmitter
    {
        public static void Generate(StringBuilder sb, TypeInfo info)
        {
            sb.AppendLine($"        public void Serialize<TWriter>(TWriter writer, object value) where TWriter : IBufferWriter<byte>");
            sb.AppendLine("        {");
            sb.AppendLine($"            var v = ({info.FullyQualifiedName})value;");
            foreach (var member in info.Members)
            {
                GenerateWriteField(sb, member);
            }
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
                    string payloadVar = $"_payload_{member.Name}";
                    string itemVar = $"_item_{member.Name}";
                    sb.AppendLine($"            if ({accessor}.Count > 0)");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                int {payloadVar} = 0;");
                    sb.AppendLine($"                foreach (var {itemVar} in {accessor})");
                    sb.AppendLine("                {");
                    GenerateGetSizePackedValue(sb, elemType, itemVar, "                    ", payloadVar, member.Wire);
                    sb.AppendLine("                }");
                    sb.AppendLine($"                ProtoWriter.WriteTag(writer, {order}, WireType.LengthDelimited);");
                    sb.AppendLine($"                ProtoWriter.WriteVarInt32(writer, (uint){payloadVar});");
                    sb.AppendLine($"                foreach (var {itemVar} in {accessor})");
                    sb.AppendLine("                {");
                    GenerateWritePackedValue(sb, elemType, itemVar, "                    ", member.Wire);
                    sb.AppendLine("                }");
                    sb.AppendLine("            }");
                }
                else
                {
                    string itemVar = $"_item_{member.Name}";
                    sb.AppendLine($"            foreach (var {itemVar} in {accessor})");
                    sb.AppendLine("            {");
                    GenerateWriteValue(sb, elemType, order, itemVar, "                ", member.Wire, skipDefault: false);
                    sb.AppendLine("            }");
                }
                return;
            }

            if (kind == CollectionKind.Map && keyType != null && valType != null)
            {
                string kvpVar = $"_kvp_{member.Name}";
                string payloadVar = $"_payload_{member.Name}";
                sb.AppendLine($"            foreach (var {kvpVar} in {accessor})");
                sb.AppendLine("            {");
                sb.AppendLine($"                int {payloadVar} = 0;");
                GenerateGetSizeValue(sb, keyType, 1, $"{kvpVar}.Key", "                ", payloadVar, skipDefault: false);
                GenerateGetSizeValue(sb, valType, 2, $"{kvpVar}.Value", "                ", payloadVar, skipDefault: false);
                sb.AppendLine($"                ProtoWriter.WriteTag(writer, {order}, WireType.LengthDelimited);");
                sb.AppendLine($"                ProtoWriter.WriteVarInt32(writer, (uint){payloadVar});");
                GenerateWriteValue(sb, keyType, 1, $"{kvpVar}.Key", "                ", skipDefault: false);
                GenerateWriteValue(sb, valType, 2, $"{kvpVar}.Value", "                ", skipDefault: false);
                sb.AppendLine("            }");
                return;
            }

            GenerateWriteValue(sb, member.Type, order, accessor, "            ", member.Wire);
        }

        private static void GenerateWriteValue(StringBuilder sb, ITypeSymbol type, int fieldNumber,
            string accessor, string indent, PbWire wire = PbWire.Varint, bool skipDefault = true)
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
                        sb.AppendLine($"{indent}    {WriteInt32Call(wire, fieldNumber, accessor + ".Value")};");
                    }
                    else if (skipDefault)
                    {
                        sb.AppendLine($"{indent}if ({accessor} != 0)");
                        sb.AppendLine($"{indent}    {WriteInt32Call(wire, fieldNumber, accessor)};");
                    }
                    else
                        sb.AppendLine($"{indent}{WriteInt32Call(wire, fieldNumber, accessor)};");
                    break;
                case SpecialType.System_Int64:
                    if (underlying != null)
                    {
                        sb.AppendLine($"{indent}if ({accessor}.HasValue)");
                        sb.AppendLine($"{indent}    {WriteInt64Call(wire, fieldNumber, accessor + ".Value")};");
                    }
                    else if (skipDefault)
                    {
                        sb.AppendLine($"{indent}if ({accessor} != 0)");
                        sb.AppendLine($"{indent}    {WriteInt64Call(wire, fieldNumber, accessor)};");
                    }
                    else
                        sb.AppendLine($"{indent}{WriteInt64Call(wire, fieldNumber, accessor)};");
                    break;
                case SpecialType.System_UInt32:
                    if (underlying != null)
                    {
                        sb.AppendLine($"{indent}if ({accessor}.HasValue)");
                        sb.AppendLine($"{indent}    {WriteUInt32Call(wire, fieldNumber, accessor + ".Value")};");
                    }
                    else if (skipDefault)
                    {
                        sb.AppendLine($"{indent}if ({accessor} != 0)");
                        sb.AppendLine($"{indent}    {WriteUInt32Call(wire, fieldNumber, accessor)};");
                    }
                    else
                        sb.AppendLine($"{indent}{WriteUInt32Call(wire, fieldNumber, accessor)};");
                    break;
                case SpecialType.System_UInt64:
                    if (underlying != null)
                    {
                        sb.AppendLine($"{indent}if ({accessor}.HasValue)");
                        sb.AppendLine($"{indent}    {WriteUInt64Call(wire, fieldNumber, accessor + ".Value")};");
                    }
                    else if (skipDefault)
                    {
                        sb.AppendLine($"{indent}if ({accessor} != 0)");
                        sb.AppendLine($"{indent}    {WriteUInt64Call(wire, fieldNumber, accessor)};");
                    }
                    else
                        sb.AppendLine($"{indent}{WriteUInt64Call(wire, fieldNumber, accessor)};");
                    break;
                case SpecialType.System_Single:
                    if (underlying != null)
                    {
                        sb.AppendLine($"{indent}if ({accessor}.HasValue)");
                        sb.AppendLine($"{indent}    ProtoWriter.WriteFloat(writer, {fieldNumber}, {accessor}.Value);");
                    }
                    else if (skipDefault)
                    {
                        sb.AppendLine($"{indent}if ({accessor} != 0f)");
                        sb.AppendLine($"{indent}    ProtoWriter.WriteFloat(writer, {fieldNumber}, {accessor});");
                    }
                    else
                        sb.AppendLine($"{indent}ProtoWriter.WriteFloat(writer, {fieldNumber}, {accessor});");
                    break;
                case SpecialType.System_Double:
                    if (underlying != null)
                    {
                        sb.AppendLine($"{indent}if ({accessor}.HasValue)");
                        sb.AppendLine($"{indent}    ProtoWriter.WriteDouble(writer, {fieldNumber}, {accessor}.Value);");
                    }
                    else if (skipDefault)
                    {
                        sb.AppendLine($"{indent}if ({accessor} != 0d)");
                        sb.AppendLine($"{indent}    ProtoWriter.WriteDouble(writer, {fieldNumber}, {accessor});");
                    }
                    else
                        sb.AppendLine($"{indent}ProtoWriter.WriteDouble(writer, {fieldNumber}, {accessor});");
                    break;
                case SpecialType.System_Boolean:
                    if (underlying != null)
                    {
                        sb.AppendLine($"{indent}if ({accessor}.HasValue)");
                        sb.AppendLine($"{indent}    ProtoWriter.WriteBool(writer, {fieldNumber}, {accessor}.Value);");
                    }
                    else if (skipDefault)
                    {
                        sb.AppendLine($"{indent}if ({accessor})");
                        sb.AppendLine($"{indent}    ProtoWriter.WriteBool(writer, {fieldNumber}, {accessor});");
                    }
                    else
                        sb.AppendLine($"{indent}ProtoWriter.WriteBool(writer, {fieldNumber}, {accessor});");
                    break;
                case SpecialType.System_String:
                    if (skipDefault)
                    {
                        sb.AppendLine($"{indent}if (!string.IsNullOrEmpty({accessor}))");
                        sb.AppendLine($"{indent}    ProtoWriter.WriteString(writer, {fieldNumber}, {accessor});");
                    }
                    else
                    {
                        sb.AppendLine($"{indent}if ({accessor} != null)");
                        sb.AppendLine($"{indent}    ProtoWriter.WriteString(writer, {fieldNumber}, {accessor});");
                    }
                    break;
                default:
                    if (SymbolParser.IsByteArray(type))
                    {
                        if (skipDefault)
                        {
                            sb.AppendLine($"{indent}if ({accessor} != null && {accessor}.Length > 0)");
                            sb.AppendLine($"{indent}    ProtoWriter.WriteBytes(writer, {fieldNumber}, {accessor});");
                        }
                        else
                        {
                            sb.AppendLine($"{indent}if ({accessor} != null)");
                            sb.AppendLine($"{indent}    ProtoWriter.WriteBytes(writer, {fieldNumber}, {accessor});");
                        }
                        break;
                    }

                    var enumType = SymbolParser.GetEnumType(type);
                    if (enumType != null)
                    {
                        if (underlying != null)
                        {
                            sb.AppendLine($"{indent}if ({accessor}.HasValue)");
                            sb.AppendLine($"{indent}    {WriteInt32Call(wire, fieldNumber, $"(int){accessor}.Value")};");
                        }
                        else if (skipDefault)
                        {
                            sb.AppendLine($"{indent}if ((int){accessor} != 0)");
                            sb.AppendLine($"{indent}    {WriteInt32Call(wire, fieldNumber, $"(int){accessor}")};");
                        }
                        else
                            sb.AppendLine($"{indent}{WriteInt32Call(wire, fieldNumber, $"(int){accessor}")};");
                        break;
                    }

                    if (type.TypeKind == TypeKind.Class && SymbolParser.HasProtoContract(type))
                    {
                        string nestedSerializer = SymbolParser.GetSerializerFullName(type);
                        string payloadVar = $"_payload_{fieldNumber}";
                        sb.AppendLine($"{indent}if ({accessor} != null)");
                        sb.AppendLine($"{indent}{{");
                        sb.AppendLine($"{indent}    int {payloadVar} = {nestedSerializer}.Instance.GetSize({accessor});");
                        sb.AppendLine($"{indent}    ProtoWriter.WriteTag(writer, {fieldNumber}, WireType.LengthDelimited);");
                        sb.AppendLine($"{indent}    ProtoWriter.WriteVarInt32(writer, (uint){payloadVar});");
                        sb.AppendLine($"{indent}    {nestedSerializer}.Instance.Serialize(writer, {accessor});");
                        sb.AppendLine($"{indent}}}");
                    }
                    else
                    {
                        sb.AppendLine($"{indent} // Unsupported type: {typeName}");
                    }
                    break;
            }
        }

        // ─── GetSize generation ───────────────────────────────────

        private static void GenerateGetSizeValue(StringBuilder sb, ITypeSymbol type, int fieldNumber,
            string accessor, string indent, string counterVar, PbWire wire = PbWire.Varint, bool skipDefault = true)
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
                        sb.AppendLine($"{indent}    {counterVar} += {SizeInt32Call(wire, fieldNumber, accessor + ".Value")};");
                    }
                    else if (skipDefault)
                    {
                        sb.AppendLine($"{indent}if ({accessor} != 0)");
                        sb.AppendLine($"{indent}    {counterVar} += {SizeInt32Call(wire, fieldNumber, accessor)};");
                    }
                    else
                        sb.AppendLine($"{indent}{counterVar} += {SizeInt32Call(wire, fieldNumber, accessor)};");
                    break;
                case SpecialType.System_Int64:
                    if (underlying != null)
                    {
                        sb.AppendLine($"{indent}if ({accessor}.HasValue)");
                        sb.AppendLine($"{indent}    {counterVar} += {SizeInt64Call(wire, fieldNumber, accessor + ".Value")};");
                    }
                    else if (skipDefault)
                    {
                        sb.AppendLine($"{indent}if ({accessor} != 0)");
                        sb.AppendLine($"{indent}    {counterVar} += {SizeInt64Call(wire, fieldNumber, accessor)};");
                    }
                    else
                        sb.AppendLine($"{indent}{counterVar} += {SizeInt64Call(wire, fieldNumber, accessor)};");
                    break;
                case SpecialType.System_UInt32:
                    if (underlying != null)
                    {
                        sb.AppendLine($"{indent}if ({accessor}.HasValue)");
                        sb.AppendLine($"{indent}    {counterVar} += {SizeUInt32Call(wire, fieldNumber, accessor + ".Value")};");
                    }
                    else if (skipDefault)
                    {
                        sb.AppendLine($"{indent}if ({accessor} != 0)");
                        sb.AppendLine($"{indent}    {counterVar} += {SizeUInt32Call(wire, fieldNumber, accessor)};");
                    }
                    else
                        sb.AppendLine($"{indent}{counterVar} += {SizeUInt32Call(wire, fieldNumber, accessor)};");
                    break;
                case SpecialType.System_UInt64:
                    if (underlying != null)
                    {
                        sb.AppendLine($"{indent}if ({accessor}.HasValue)");
                        sb.AppendLine($"{indent}    {counterVar} += {SizeUInt64Call(wire, fieldNumber, accessor + ".Value")};");
                    }
                    else if (skipDefault)
                    {
                        sb.AppendLine($"{indent}if ({accessor} != 0)");
                        sb.AppendLine($"{indent}    {counterVar} += {SizeUInt64Call(wire, fieldNumber, accessor)};");
                    }
                    else
                        sb.AppendLine($"{indent}{counterVar} += {SizeUInt64Call(wire, fieldNumber, accessor)};");
                    break;
                case SpecialType.System_Single:
                    if (underlying != null)
                    {
                        sb.AppendLine($"{indent}if ({accessor}.HasValue)");
                        sb.AppendLine($"{indent}    {counterVar} += ProtoWriter.GetTagSize({fieldNumber}, WireType.Fixed32) + 4;");
                    }
                    else if (skipDefault)
                    {
                        sb.AppendLine($"{indent}if ({accessor} != 0f)");
                        sb.AppendLine($"{indent}    {counterVar} += ProtoWriter.GetTagSize({fieldNumber}, WireType.Fixed32) + 4;");
                    }
                    else
                        sb.AppendLine($"{indent}{counterVar} += ProtoWriter.GetTagSize({fieldNumber}, WireType.Fixed32) + 4;");
                    break;
                case SpecialType.System_Double:
                    if (underlying != null)
                    {
                        sb.AppendLine($"{indent}if ({accessor}.HasValue)");
                        sb.AppendLine($"{indent}    {counterVar} += ProtoWriter.GetTagSize({fieldNumber}, WireType.Fixed64) + 8;");
                    }
                    else if (skipDefault)
                    {
                        sb.AppendLine($"{indent}if ({accessor} != 0d)");
                        sb.AppendLine($"{indent}    {counterVar} += ProtoWriter.GetTagSize({fieldNumber}, WireType.Fixed64) + 8;");
                    }
                    else
                        sb.AppendLine($"{indent}{counterVar} += ProtoWriter.GetTagSize({fieldNumber}, WireType.Fixed64) + 8;");
                    break;
                case SpecialType.System_Boolean:
                    if (underlying != null)
                    {
                        sb.AppendLine($"{indent}if ({accessor}.HasValue)");
                        sb.AppendLine($"{indent}    {counterVar} += ProtoWriter.GetTagSize({fieldNumber}, WireType.Varint) + 1;");
                    }
                    else if (skipDefault)
                    {
                        sb.AppendLine($"{indent}if ({accessor})");
                        sb.AppendLine($"{indent}    {counterVar} += ProtoWriter.GetTagSize({fieldNumber}, WireType.Varint) + 1;");
                    }
                    else
                        sb.AppendLine($"{indent}{counterVar} += ProtoWriter.GetTagSize({fieldNumber}, WireType.Varint) + 1;");
                    break;
                case SpecialType.System_String:
                    if (skipDefault)
                    {
                        sb.AppendLine($"{indent}if (!string.IsNullOrEmpty({accessor}))");
                        sb.AppendLine($"{indent}    {counterVar} += ProtoWriter.GetTagSize({fieldNumber}, WireType.LengthDelimited) + ProtoWriter.GetVarInt32Size((uint)ProtoWriter.GetStringSize({accessor})) + ProtoWriter.GetStringSize({accessor});");
                    }
                    else
                    {
                        sb.AppendLine($"{indent}if ({accessor} != null)");
                        sb.AppendLine($"{indent}    {counterVar} += ProtoWriter.GetTagSize({fieldNumber}, WireType.LengthDelimited) + ProtoWriter.GetVarInt32Size((uint)ProtoWriter.GetStringSize({accessor})) + ProtoWriter.GetStringSize({accessor});");
                    }
                    break;
                default:
                    if (SymbolParser.IsByteArray(type))
                    {
                        if (skipDefault)
                        {
                            sb.AppendLine($"{indent}if ({accessor} != null && {accessor}.Length > 0)");
                            sb.AppendLine($"{indent}    {counterVar} += ProtoWriter.GetTagSize({fieldNumber}, WireType.LengthDelimited) + ProtoWriter.GetVarInt32Size((uint){accessor}.Length) + {accessor}.Length;");
                        }
                        else
                        {
                            sb.AppendLine($"{indent}if ({accessor} != null)");
                            sb.AppendLine($"{indent}    {counterVar} += ProtoWriter.GetTagSize({fieldNumber}, WireType.LengthDelimited) + ProtoWriter.GetVarInt32Size((uint){accessor}.Length) + {accessor}.Length;");
                        }
                        break;
                    }

                    var enumType = SymbolParser.GetEnumType(type);
                    if (enumType != null)
                    {
                        if (underlying != null)
                        {
                            sb.AppendLine($"{indent}if ({accessor}.HasValue)");
                            sb.AppendLine($"{indent}    {counterVar} += {SizeInt32Call(wire, fieldNumber, $"(int){accessor}.Value")};");
                        }
                        else if (skipDefault)
                        {
                            sb.AppendLine($"{indent}if ((int){accessor} != 0)");
                            sb.AppendLine($"{indent}    {counterVar} += {SizeInt32Call(wire, fieldNumber, $"(int){accessor}")};");
                        }
                        else
                            sb.AppendLine($"{indent}{counterVar} += {SizeInt32Call(wire, fieldNumber, $"(int){accessor}")};");
                        break;
                    }

                    if (type.TypeKind == TypeKind.Class && SymbolParser.HasProtoContract(type))
                    {
                        string nestedSerializer = SymbolParser.GetSerializerFullName(type);
                        string payloadVar = $"_sz_{fieldNumber}";
                        sb.AppendLine($"{indent}if ({accessor} != null)");
                        sb.AppendLine($"{indent}{{");
                        sb.AppendLine($"{indent}    int {payloadVar} = {nestedSerializer}.Instance.GetSize({accessor});");
                        sb.AppendLine($"{indent}    {counterVar} += ProtoWriter.GetTagSize({fieldNumber}, WireType.LengthDelimited) + ProtoWriter.GetVarInt32Size((uint){payloadVar}) + {payloadVar};");
                        sb.AppendLine($"{indent}}}");
                    }
                    break;
            }
        }

        private static void GenerateGetSizePackedValue(StringBuilder sb, ITypeSymbol type,
            string accessor, string indent, string counterVar, PbWire wire)
        {
            ITypeSymbol? underlying = SymbolParser.GetNullableUnderlyingType(type);
            ITypeSymbol effective = underlying ?? type;
            SpecialType st = effective.SpecialType;

            string valueExpr = underlying != null ? $"{accessor}.Value" : accessor;

            if (effective.TypeKind == TypeKind.Enum)
            {
                valueExpr = $"(int){valueExpr}";
                st = SpecialType.System_Int32;
            }

            switch (st)
            {
                case SpecialType.System_Int32:
                    if (wire == PbWire.ZigZag)
                        sb.AppendLine($"{indent}{counterVar} += ProtoWriter.GetVarInt32Size(ProtoWriter.ZigZag32({valueExpr}));");
                    else if (wire == PbWire.Fixed32)
                        sb.AppendLine($"{indent}{counterVar} += 4;");
                    else
                        sb.AppendLine($"{indent}{counterVar} += ProtoWriter.GetVarInt64Size((ulong)(long){valueExpr});");
                    break;
                case SpecialType.System_Int64:
                    if (wire == PbWire.ZigZag)
                        sb.AppendLine($"{indent}{counterVar} += ProtoWriter.GetVarInt64Size(ProtoWriter.ZigZag64({valueExpr}));");
                    else if (wire == PbWire.Fixed64)
                        sb.AppendLine($"{indent}{counterVar} += 8;");
                    else
                        sb.AppendLine($"{indent}{counterVar} += ProtoWriter.GetVarInt64Size((ulong)(long){valueExpr});");
                    break;
                case SpecialType.System_UInt32:
                    if (wire == PbWire.Fixed32)
                        sb.AppendLine($"{indent}{counterVar} += 4;");
                    else
                        sb.AppendLine($"{indent}{counterVar} += ProtoWriter.GetVarInt32Size({valueExpr});");
                    break;
                case SpecialType.System_UInt64:
                    if (wire == PbWire.Fixed64)
                        sb.AppendLine($"{indent}{counterVar} += 8;");
                    else
                        sb.AppendLine($"{indent}{counterVar} += ProtoWriter.GetVarInt64Size({valueExpr});");
                    break;
                case SpecialType.System_Single:
                    sb.AppendLine($"{indent}{counterVar} += 4;");
                    break;
                case SpecialType.System_Double:
                    sb.AppendLine($"{indent}{counterVar} += 8;");
                    break;
                case SpecialType.System_Boolean:
                    sb.AppendLine($"{indent}{counterVar} += 1;");
                    break;
            }
        }

        public static void GenerateGetSize(StringBuilder sb, TypeInfo info)
        {
            sb.AppendLine("        public int GetSize(object value)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var v = ({info.FullyQualifiedName})value;");
            sb.AppendLine("            int size = 0;");
            foreach (var member in info.Members)
            {
                GenerateGetSizeField(sb, member);
            }
            sb.AppendLine("            return size;");
            sb.AppendLine("        }");
        }

        private static void GenerateGetSizeField(StringBuilder sb, MemberInfo member)
        {
            int order = member.Order;
            string accessor = $"v.{member.Name}";

            var kind = SymbolParser.GetCollectionInfo(member.Type, out var elemType, out var keyType, out var valType);

            if (kind == CollectionKind.List && elemType != null)
            {
                bool isPacked = IsPackedType(elemType);

                if (isPacked)
                {
                    string payloadVar = $"_psz_{member.Name}";
                    string itemVar = $"_item_{member.Name}";
                    sb.AppendLine($"            if ({accessor}.Count > 0)");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                int {payloadVar} = 0;");
                    sb.AppendLine($"                foreach (var {itemVar} in {accessor})");
                    sb.AppendLine("                {");
                    GenerateGetSizePackedValue(sb, elemType, itemVar, "                    ", payloadVar, member.Wire);
                    sb.AppendLine("                }");
                    sb.AppendLine($"                size += ProtoWriter.GetTagSize({order}, WireType.LengthDelimited) + ProtoWriter.GetVarInt32Size((uint){payloadVar}) + {payloadVar};");
                    sb.AppendLine("            }");
                }
                else
                {
                    string itemVar = $"_item_{member.Name}";
                    sb.AppendLine($"            foreach (var {itemVar} in {accessor})");
                    sb.AppendLine("            {");
                    GenerateGetSizeValue(sb, elemType, order, itemVar, "                ", "size", member.Wire, skipDefault: false);
                    sb.AppendLine("            }");
                }
                return;
            }

            if (kind == CollectionKind.Map && keyType != null && valType != null)
            {
                string kvpVar = $"_kvp_{member.Name}";
                string payloadVar = $"_psz_{member.Name}";
                sb.AppendLine($"            foreach (var {kvpVar} in {accessor})");
                sb.AppendLine("            {");
                sb.AppendLine($"                int {payloadVar} = 0;");
                GenerateGetSizeValue(sb, keyType, 1, $"{kvpVar}.Key", "                ", payloadVar, skipDefault: false);
                GenerateGetSizeValue(sb, valType, 2, $"{kvpVar}.Value", "                ", payloadVar, skipDefault: false);
                sb.AppendLine($"                size += ProtoWriter.GetTagSize({order}, WireType.LengthDelimited) + ProtoWriter.GetVarInt32Size((uint){payloadVar}) + {payloadVar};");
                sb.AppendLine("            }");
                return;
            }

            GenerateGetSizeValue(sb, member.Type, order, accessor, "            ", "size", member.Wire);
        }

        // ─── Size helpers ────────────────────────────────────────

        private static string SizeInt32Call(PbWire wire, int fieldNumber, string accessor) => wire switch
        {
            PbWire.ZigZag  => $"ProtoWriter.GetTagSize({fieldNumber}, WireType.Varint) + ProtoWriter.GetVarInt32Size(ProtoWriter.ZigZag32({accessor}))",
            PbWire.Fixed32 => $"ProtoWriter.GetTagSize({fieldNumber}, WireType.Fixed32) + 4",
            _ => $"ProtoWriter.GetTagSize({fieldNumber}, WireType.Varint) + ProtoWriter.GetVarInt64Size((ulong)(long){accessor})",
        };

        private static string SizeInt64Call(PbWire wire, int fieldNumber, string accessor) => wire switch
        {
            PbWire.ZigZag  => $"ProtoWriter.GetTagSize({fieldNumber}, WireType.Varint) + ProtoWriter.GetVarInt64Size(ProtoWriter.ZigZag64({accessor}))",
            PbWire.Fixed64 => $"ProtoWriter.GetTagSize({fieldNumber}, WireType.Fixed64) + 8",
            _ => $"ProtoWriter.GetTagSize({fieldNumber}, WireType.Varint) + ProtoWriter.GetVarInt64Size((ulong)(long){accessor})",
        };

        private static string SizeUInt32Call(PbWire wire, int fieldNumber, string accessor) => wire switch
        {
            PbWire.Fixed32 => $"ProtoWriter.GetTagSize({fieldNumber}, WireType.Fixed32) + 4",
            _ => $"ProtoWriter.GetTagSize({fieldNumber}, WireType.Varint) + ProtoWriter.GetVarInt32Size({accessor})",
        };

        private static string SizeUInt64Call(PbWire wire, int fieldNumber, string accessor) => wire switch
        {
            PbWire.Fixed64 => $"ProtoWriter.GetTagSize({fieldNumber}, WireType.Fixed64) + 8",
            _ => $"ProtoWriter.GetTagSize({fieldNumber}, WireType.Varint) + ProtoWriter.GetVarInt64Size({accessor})",
        };

        // ─── Write helpers ───────────────────────────────────────

        private static string WriteInt32Call(PbWire wire, int fieldNumber, string accessor) => wire switch
        {
            PbWire.ZigZag  => $"ProtoWriter.WriteSInt32(writer, {fieldNumber}, {accessor})",
            PbWire.Fixed32 => $"ProtoWriter.WriteSFixed32(writer, {fieldNumber}, {accessor})",
            _ => $"ProtoWriter.WriteInt32(writer, {fieldNumber}, {accessor})",
        };

        private static string WriteInt64Call(PbWire wire, int fieldNumber, string accessor) => wire switch
        {
            PbWire.ZigZag  => $"ProtoWriter.WriteSInt64(writer, {fieldNumber}, {accessor})",
            PbWire.Fixed64 => $"ProtoWriter.WriteSFixed64(writer, {fieldNumber}, {accessor})",
            _ => $"ProtoWriter.WriteInt64(writer, {fieldNumber}, {accessor})",
        };

        private static string WriteUInt32Call(PbWire wire, int fieldNumber, string accessor) => wire switch
        {
            PbWire.Fixed32 => $"ProtoWriter.WriteFixed32(writer, {fieldNumber}, (int){accessor})",
            _ => $"ProtoWriter.WriteUInt32(writer, {fieldNumber}, {accessor})",
        };

        private static string WriteUInt64Call(PbWire wire, int fieldNumber, string accessor) => wire switch
        {
            PbWire.Fixed64 => $"ProtoWriter.WriteFixed64(writer, {fieldNumber}, (long){accessor})",
            _ => $"ProtoWriter.WriteUInt64(writer, {fieldNumber}, {accessor})",
        };

        private static bool IsPackedType(ITypeSymbol type)
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

        private static void GenerateWritePackedValue(StringBuilder sb, ITypeSymbol type,
            string accessor, string indent, PbWire wire)
        {
            ITypeSymbol? underlying = SymbolParser.GetNullableUnderlyingType(type);
            ITypeSymbol effective = underlying ?? type;
            SpecialType st = effective.SpecialType;

            string valueExpr = underlying != null ? $"{accessor}.Value" : accessor;

            if (effective.TypeKind == TypeKind.Enum)
            {
                valueExpr = $"(int){valueExpr}";
                st = SpecialType.System_Int32;
            }

            switch (st)
            {
                case SpecialType.System_Int32:
                    if (wire == PbWire.ZigZag)
                        sb.AppendLine($"{indent}ProtoWriter.WriteVarInt32(writer, ProtoWriter.ZigZag32({valueExpr}));");
                    else if (wire == PbWire.Fixed32)
                        sb.AppendLine($"{indent}ProtoWriter.WriteFixed32(writer, (uint){valueExpr});");
                    else
                        sb.AppendLine($"{indent}ProtoWriter.WriteVarInt64(writer, (ulong)(long){valueExpr});");
                    break;
                case SpecialType.System_Int64:
                    if (wire == PbWire.ZigZag)
                        sb.AppendLine($"{indent}ProtoWriter.WriteVarInt64(writer, ProtoWriter.ZigZag64({valueExpr}));");
                    else if (wire == PbWire.Fixed64)
                        sb.AppendLine($"{indent}ProtoWriter.WriteFixed64(writer, (ulong){valueExpr});");
                    else
                        sb.AppendLine($"{indent}ProtoWriter.WriteVarInt64(writer, (ulong)(long){valueExpr});");
                    break;
                case SpecialType.System_UInt32:
                    if (wire == PbWire.Fixed32)
                        sb.AppendLine($"{indent}ProtoWriter.WriteFixed32(writer, {valueExpr});");
                    else
                        sb.AppendLine($"{indent}ProtoWriter.WriteVarInt32(writer, (uint){valueExpr});");
                    break;
                case SpecialType.System_UInt64:
                    if (wire == PbWire.Fixed64)
                        sb.AppendLine($"{indent}ProtoWriter.WriteFixed64(writer, {valueExpr});");
                    else
                        sb.AppendLine($"{indent}ProtoWriter.WriteVarInt64(writer, (ulong){valueExpr});");
                    break;
                case SpecialType.System_Single:
                    sb.AppendLine($"{indent}ProtoWriter.WriteFixed32(writer, (uint)BitConverter.SingleToInt32Bits({valueExpr}));");
                    break;
                case SpecialType.System_Double:
                    sb.AppendLine($"{indent}ProtoWriter.WriteFixed64(writer, (ulong)BitConverter.DoubleToInt64Bits({valueExpr}));");
                    break;
                case SpecialType.System_Boolean:
                    sb.AppendLine($"{indent}ProtoWriter.WriteVarInt32(writer, {valueExpr} ? 1u : 0u);");
                    break;
            }
        }
    }
}
