# PbLite

[![NuGet](https://img.shields.io/nuget/v/PbLite)](https://www.nuget.org/packages/PbLite)

**面向 C# / Unity / .NET 高性能游戏场景的 Protobuf Wire Format 兼容序列化框架。**

零分配、AOT 友好，适用于游戏客户端与服务端的高频通信场景。

## ✨ Features

| 特性                          | 支持 |
| ----------------------------- | ---- |
| Zero Allocation               | ✅   |
| Unity IL2CPP / WebGL 支持    | ✅   |
| Native AOT 支持               | ✅   |
| Protobuf Wire Format 兼容     | ✅   |
| IBufferWriter<byte> 原生支持  | ✅   |
| Source Generator 静态代码生成 | ✅   |
| 无 Runtime Reflection         | ✅   |
| 无 Dynamic Code Generation    | ✅   |
| 自定义消息注册策略            | ✅   |
| proto3 → C# 代码生成 (PbGen)  | ✅   |

# 🆚 Why PbLite?

PbLite 针对这些场景重新设计：

- **编译期代码生成** — Source Generator 生成 Serializer，无 Runtime Reflection、无 Runtime Model、无 DynamicMethod
- **IBufferWriter<byte> 直接写入** — 序列化直接写入目标 Buffer（Pipelines / ArrayPool / Socket Buffer），无 MemoryStream、无临时 byte[]、无中间缓存
- **GetSize + Serialize** — 嵌套消息先通过 GetSize 计算长度再写入正确的 varint，兼容任何 IBufferWriter 实现
- **AOT 友好** — 无 Reflection / Reflection.Emit，支持 Unity IL2CPP / WebGL / iOS / Native AOT
- **灵活注册** — 不限制消息注册方式，支持 MessageId Registry 和 Type Registry

标准 Protobuf Reader 可正常读取 PbLite 生成的数据。

# Quick Start

## 1. Define Message

手写 C# class：

```csharp
[PbContract]
public partial class ChatMessage
{
    [PbMember(1)]
    public long PlayerId;

    [PbMember(2)]
    public string Content = "";
}
```

或通过 PbGen 从 `.proto` 生成：

```bash
# 安装工具
dotnet tool install -g PbLite.Gen

# 从 proto 生成 C# class
pblite-gen messages.proto -o ./Generated
```

生成的类会自动被 Source Generator 生成对应的 Serializer。

## 2. Initialize Registry

PbLite 生成 `ProtoGenerated` 静态类遍历所有 Serializer。按需创建 Registry：

```csharp
public sealed class ProtoRegistry
{
    private readonly Dictionary<Type, IProtoSerializer> _serializers = new();

    public void Register(IProtoSerializer serializer)
        => _serializers.Add(serializer.Type, serializer);

    public IProtoSerializer Get(Type type)
        => _serializers[type];
}
```

初始化：

```csharp
var registry = new ProtoRegistry();
ProtoGenerated.ForEach(registry.Register);
```

游戏服务端通常用 MessageId 而非 Type 定位消息：

```csharp
messageRegistry.Register(1001, ChatMessageSerializer.Instance);

// 通过 MsgIdMap 自动关联
ProtoGenerated.ForEach(serializer =>
{
    var msgId = MsgIdMap.Get(serializer.Type);
    messageRegistry.Add(msgId, serializer);
});
```

## 3. Serialize

```csharp
var message = new ChatMessage
{
    PlayerId = 10001,
    Content = "Hello PbLite"
};

var writer = new ArrayBufferWriter<byte>();
var serializer = registry.Get(typeof(ChatMessage));
serializer.Serialize(writer, message);
// writer.WrittenSpan 即为序列化结果
```

## 4. Deserialize

```csharp
var reader = new ProtoReader(new ReadOnlySequence<byte>(writer.WrittenSpan));
var serializer = registry.Get(typeof(ChatMessage));
var message = (ChatMessage)serializer.Deserialize(ref reader, null);
```

# proto3 兼容性

## 已支持

| proto3 特性 | 说明 |
| --- | --- |
| 标量类型 | int32/int64/uint32/uint64/sint32/sint64/fixed32/fixed64/sfixed32/sfixed64/bool/float/double/string/bytes |
| 枚举 (enum) | 生成 C# enum，序列化/反序列化为 varint，支持 repeated(packed) |
| 嵌套消息 | 扁平化为同级 C# class |
| repeated | 生成 `List<T>`，标量默认 packed |
| map\<K,V\> | 生成 `Dictionary<K,V>` |
| optional | 映射为 C# 可空类型 (`int?` / `string?`)，presence 语义 |
| sint/sfixed/fixed 编码 | 通过 `[PbMember(Wire = PbWire.ZigZag/Fixed32/Fixed64)]` 支持 |
| reserved 字段 | 解析时跳过，不生成代码 |
| proto options | `csharp_namespace`、`deprecated`（生成 `[Obsolete]`），未知 option 静默忽略 |
| import 语句 | 解析时跳过，不影响代码生成 |
| 未知字段 | 反序列化时自动跳过 |

## 不支持

| proto3 特性 | 说明 |
| --- | --- |
| oneof | 不生成代码，解析时跳过 |
| service / RPC | 不生成任何代码 |
| proto2 语法 | 不支持 |
| import 跨文件类型解析 | import 语句被跳过，不解析被导入文件的 AST，跨文件引用按全限定名映射 |
| extensions (proto2) | 解析时跳过 |
| `[packed=false]` field option | 可解析但序列化器始终按 packed 处理标量 repeated |
| 自定义 options (option (xxx)) | 解析时跳过 |
| group (deprecated) | 不支持 |

> 游戏场景下基本上用不到

# License

MIT License
