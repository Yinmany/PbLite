# PbLite

**一个面向 .NET 游戏服务器的高性能、零分配、AOT 友好的 Protobuf Wire Format 兼容序列化框架。**

PbLite 使用 Source Generator 在编译阶段生成静态序列化代码，避免运行时反射、动态代码生成以及运行时类型分析。

专为实时游戏通信场景设计：

- 高吞吐网络消息
- 低延迟通信
- 低 GC 压力
- Unity Client / .NET Server 通信

## ✨ Features

| 特性                          | 支持 |
| ----------------------------- | ---- |
| Source Generator 静态代码生成 | ✅   |
| Protobuf Wire Format 兼容     | ✅   |
| 高性能序列化                  | ✅   |
| Zero Allocation               | ✅   |
| IBufferWriter<byte> 原生支持  | ✅   |
| 自定义 Buffer Pipeline        | ✅   |
| Unity IL2CPP 支持             | ✅   |
| Unity WebGL 支持              | ✅   |
| Native AOT 支持               | ✅   |
| 无 Runtime Reflection         | ✅   |
| 无 Dynamic Code Generation    | ✅   |
| 嵌套 Message 单次遍历写入     | ✅   |
| 自定义消息注册策略            | ✅   |

# 🆚 Why PbLite?

protobuf-net 是一个优秀的通用 Protobuf 实现。

但是实时游戏服务器通常有不同的需求：

- 高频消息传输
- 大量并发连接
- 低延迟通信
- 低 GC 压力
- Unity AOT 平台限制
- 自定义网络 Buffer

PbLite 针对这些场景重新设计：

- Source Generator 编译期生成 Serializer
- 无 Runtime Reflection
- 无 Runtime Model
- 基于 `IBufferWriter<byte>` 的直接写入
- 支持自定义 Buffer Pipeline
- 针对嵌套 Message 优化序列化流程

目标：

> 一个简单、透明、高性能、面向游戏服务器的 Protobuf Wire Format 兼容序列化框架。

# 🏗 Design

PbLite 将复杂工作放到编译阶段：

```
Message Definition → Source Generator → Generated Serializer
```

运行时：

```
Object → Serializer → Bytes
```

Serializer 由代码生成，运行时无需：

- Reflection
- Assembly Scan
- Dynamic Code Generation

# ⚡ High Performance

PbLite 基于 `IBufferWriter<byte>` 设计。

序列化可以直接写入目标 Buffer：

```
Object → Serializer → IBufferWriter<byte>
```

支持：

- 自定义 ByteBuf
- System.IO.Pipelines
- ArrayPool Buffer
- Socket Buffer

避免：

- MemoryStream
- 临时 byte[]
- 中间序列化缓存
- 数据复制

适用于：

- Entity Sync
- Combat Event
- Chat Message
- Player State
- Scene Update

# 🔥 Optimized Nested Message

传统 Protobuf 嵌套 Message：

```
Tag
Length
Message
```

通常需要提前计算长度：

```
GetSize()
+
Serialize()
```

导致额外遍历。

PbLite 使用固定长度 VarInt 占位方式：

```
Tag + Length(5 Bytes) + Message
```

写入流程：

```
Reserve Length → Serialize Child → Patch Length
```

优势：

- 单次遍历
- 无 Size Pass
- 无临时 Buffer
- 无额外 Copy

同时保持 Protobuf Wire Format 兼容。

标准 Protobuf Reader 可以正常读取 PbLite 生成的数据。

# 🧬 AOT Friendly

PbLite 不依赖：

- Reflection
- Reflection.Emit
- DynamicMethod
- Runtime Code Generation

支持：

- Unity IL2CPP
- Unity WebGL
- iOS
- Native AOT

适合 Unity 游戏客户端与 .NET 游戏服务器通信。

# 🔌 Flexible Registry

PbLite 只负责：

```
Message → Serializer → Bytes
```

不限制消息如何注册。

用户可以根据项目需求选择不同方式。

## MessageId Registry

适合：

- 游戏网络协议
- Gateway
- RPC

```
MessageId → Serializer
```

例如：

```csharp
registry.Register(1001, LoginReqSerializer.Instance);
```

## Type Registry

适合：

- RPC
- 数据存储
- 工具系统

```
Type → Serializer
```

例如：

```csharp
registry.Register(typeof(LoginReq), LoginReqSerializer.Instance);
```

# 📦 Lightweight Runtime

PbLite Runtime：

```
PbLite

├── ProtoReader
├── ProtoWriter
├── IProtoSerializer
├── Source Generator
└── Generated Serializer
```

没有：

- Runtime Model
- Metadata Cache
- Reflection Pipeline

运行阶段只负责：

```
Encode / Decode / Buffer Read / Write
```

# 🎮 Designed For Game Servers

PbLite 适用于：

- MMORPG
- 实时对战游戏
- 房间制游戏
- 卡牌游戏
- ECS Server
- Unity Multiplayer

典型通信：

```
Unity Client → TCP/WebSocket/KCP → Gateway → Game Server → PbLite
```

# Quick Start

## 1. Define Message

定义一个消息类型：

```csharp
[PbContract]
public partial class ChatMessage
{
    [PbMember(1)]
    public long PlayerId;

    [PbMember(2)]
    public string Content;
}
```

---

## 2. Initialize Serializer Registry

PbLite 会生成 `ProtoGenerated` 静态类，用于遍历所有生成的 Serializer。

用户可以根据自己的需求创建 Registry。

例如使用 `Type` 作为 Key：

```csharp
public sealed class ProtoRegistry
{
    private readonly Dictionary<Type, IProtoSerializer> _serializers = new();

    public void Register(IProtoSerializer serializer)
    {
        _serializers.Add(serializer.Type, serializer);
    }

    public IProtoSerializer Get(Type type)
    {
        return _serializers[type];
    }
}
```

初始化：

```csharp
var registry = new ProtoRegistry();
ProtoGenerated.ForEach(registry.Register);
```

---

## 3. Serialize

通过 Type 获取 Serializer：

```csharp
var message = new ChatMessage
{
    PlayerId = 10001,
    Content = "Hello PbLite"
};

var serializer = registry.Get(typeof(ChatMessage));
serializer.Serialize(writer, message);
```

数据会直接写入 `IBufferWriter<byte>`：

```text
Object → Serializer → IBufferWriter<byte>
```

---

## 4. Deserialize

反序列化时指定类型：

```csharp
var serializer = registry.Get(typeof(ChatMessage));
var message = serializer.Deserialize(ref reader);
```

得到：

```csharp
ChatMessage message
```

---

## 5. Custom Registry

PbLite 不限制消息定位方式。

除了 Type Registry，也可以根据项目需求使用：

```text
MessageId → Serializer
```

例如游戏服务器：

```csharp
messageRegistry.Register(1001, ChatMessageSerializer.Instance);

// 通过MsgIdMap进行自动关联
ProtoGenerated.ForEach(serializer =>
{
    var msgId = MsgIdMap.Get(serializer.Type);
    messageRegistry.Add(msgId, serializer);
});
```

# License

MIT License
