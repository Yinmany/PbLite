using System.Buffers;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using PbLite;
using ProtoBuf;

namespace PbLite.Bench;

// ─── PbLite 消息定义 ──────────────────────────────────────

[PbContract]
public class BenchInner
{
    [PbMember(1)] public int Id { get; set; }
    [PbMember(2)] public string Name { get; set; } = "";
}

[PbContract]
public class BenchMessage
{
    [PbMember(1)] public int Int32Field { get; set; }
    [PbMember(2)] public long Int64Field { get; set; }
    [PbMember(3)] public string StringField { get; set; } = "";
    [PbMember(4)] public float FloatField { get; set; }
    [PbMember(5)] public double DoubleField { get; set; }
    [PbMember(6)] public bool BoolField { get; set; }
    [PbMember(7)] public BenchInner? Nested { get; set; }
    [PbMember(8)] public List<int> IntList { get; set; } = new();
    [PbMember(9)] public List<string> StringList { get; set; } = new();
    [PbMember(10)] public List<BenchInner> MessageList { get; set; } = new();
    [PbMember(11)] public Dictionary<int, string> Map { get; set; } = new();
}

// ─── protobuf-net 对照定义 ────────────────────────────────

[ProtoContract]
public class PbBenchInner
{
    [ProtoMember(1)] public int Id { get; set; }
    [ProtoMember(2)] public string Name { get; set; } = "";
}

[ProtoContract]
public class PbBenchMessage
{
    [ProtoMember(1)] public int Int32Field { get; set; }
    [ProtoMember(2)] public long Int64Field { get; set; }
    [ProtoMember(3)] public string StringField { get; set; } = "";
    [ProtoMember(4)] public float FloatField { get; set; }
    [ProtoMember(5)] public double DoubleField { get; set; }
    [ProtoMember(6)] public bool BoolField { get; set; }
    [ProtoMember(7)] public PbBenchInner? Nested { get; set; }
    [ProtoMember(8)] public List<int> IntList { get; set; } = new();
    [ProtoMember(9)] public List<string> StringList { get; set; } = new();
    [ProtoMember(10)] public List<PbBenchInner> MessageList { get; set; } = new();
    [ProtoMember(11)] public Dictionary<int, string> Map { get; set; } = new();
}

// ─── Benchmark ───────────────────────────────────────────

[MemoryDiagnoser]
public class SerializeBenchmarks
{
    private BenchMessage _pbliteMsg = null!;
    private PbBenchMessage _pbnetMsg = null!;
    private ArrayBufferWriter<byte> _writer = null!;
    private MemoryStream _pbnetStream = null!;

    [Params("small", "large")]
    public string Scenario { get; set; } = "small";

    [GlobalSetup]
    public void Setup()
    {
        _pbliteMsg = new BenchMessage
        {
            Int32Field = -12345,
            Int64Field = -9876543210L,
            StringField = "Hello PbLite Benchmark",
            FloatField = 3.14f,
            DoubleField = 2.718281828459045,
            BoolField = true,
            Nested = new BenchInner { Id = 777, Name = "Nested" },
            IntList = { 1, 2, 3, 100, -50 },
            StringList = { "a", "bb", "ccc" },
            MessageList =
            {
                new BenchInner { Id = 1, Name = "first" },
                new BenchInner { Id = 2, Name = "second" }
            },
            Map = { [10] = "ten", [20] = "twenty" }
        };
        _pbnetMsg = new PbBenchMessage
        {
            Int32Field = -12345,
            Int64Field = -9876543210L,
            StringField = "Hello PbLite Benchmark",
            FloatField = 3.14f,
            DoubleField = 2.718281828459045,
            BoolField = true,
            Nested = new PbBenchInner { Id = 777, Name = "Nested" },
            IntList = { 1, 2, 3, 100, -50 },
            StringList = { "a", "bb", "ccc" },
            MessageList =
            {
                new PbBenchInner { Id = 1, Name = "first" },
                new PbBenchInner { Id = 2, Name = "second" }
            },
            Map = { [10] = "ten", [20] = "twenty" }
        };

        if (Scenario == "large")
        {
            for (int i = 0; i < 100; i++)
            {
                _pbliteMsg.IntList.Add(i);
                _pbliteMsg.StringList.Add($"item_{i}");
                _pbliteMsg.MessageList.Add(new BenchInner { Id = i, Name = $"name_{i}" });
                _pbliteMsg.Map[i] = $"val_{i}";
            }
            for (int i = 0; i < 100; i++)
            {
                _pbnetMsg.IntList.Add(i);
                _pbnetMsg.StringList.Add($"item_{i}");
                _pbnetMsg.MessageList.Add(new PbBenchInner { Id = i, Name = $"name_{i}" });
                _pbnetMsg.Map[i] = $"val_{i}";
            }
        }

        // 预热 protobuf-net 类型模型（首次调用触发反射/JIT）
        var warmupMs = new MemoryStream();
        Serializer.Serialize(warmupMs, _pbnetMsg);
        Serializer.Deserialize<PbBenchMessage>(new MemoryStream(warmupMs.ToArray()));

        // 试序列化一次以确定实际大小，按实际大小预分配 buffer 避免扩容
        var probeWriter = new ArrayBufferWriter<byte>();
        BenchMessageSerializer.Instance.Serialize(probeWriter, _pbliteMsg);
        int pbliteSize = probeWriter.WrittenCount;

        _writer = new ArrayBufferWriter<byte>(pbliteSize);
        _pbnetStream = new MemoryStream(pbliteSize);
    }

    [Benchmark(Description = "PbLite Serialize")]
    public void PbLiteSerialize()
    {
        _writer.Clear();
        BenchMessageSerializer.Instance.Serialize(_writer, _pbliteMsg);
    }

    [Benchmark(Description = "protobuf-net Serialize")]
    public void ProtobufNetSerialize()
    {
        _pbnetStream.SetLength(0);
        Serializer.Serialize(_pbnetStream, _pbnetMsg);
    }
}

[MemoryDiagnoser]
public class DeserializeBenchmarks
{
    private byte[] _pbliteData = null!;
    private byte[] _pbnetData = null!;

    [Params("small", "large")]
    public string Scenario { get; set; } = "small";

    [GlobalSetup]
    public void Setup()
    {
        var msg = new BenchMessage
        {
            Int32Field = -12345,
            Int64Field = -9876543210L,
            StringField = "Hello PbLite Benchmark",
            FloatField = 3.14f,
            DoubleField = 2.718281828459045,
            BoolField = true,
            Nested = new BenchInner { Id = 777, Name = "Nested" },
            IntList = { 1, 2, 3, 100, -50 },
            StringList = { "a", "bb", "ccc" },
            MessageList =
            {
                new BenchInner { Id = 1, Name = "first" },
                new BenchInner { Id = 2, Name = "second" }
            },
            Map = { [10] = "ten", [20] = "twenty" }
        };

        if (Scenario == "large")
        {
            for (int i = 0; i < 100; i++)
            {
                msg.IntList.Add(i);
                msg.StringList.Add($"item_{i}");
                msg.MessageList.Add(new BenchInner { Id = i, Name = $"name_{i}" });
                msg.Map[i] = $"val_{i}";
            }
        }

        var writer = new ArrayBufferWriter<byte>(4096);
        BenchMessageSerializer.Instance.Serialize(writer, msg);
        _pbliteData = writer.WrittenSpan.ToArray();

        var pbMsg = new PbBenchMessage
        {
            Int32Field = msg.Int32Field,
            Int64Field = msg.Int64Field,
            StringField = msg.StringField,
            FloatField = msg.FloatField,
            DoubleField = msg.DoubleField,
            BoolField = msg.BoolField,
            Nested = new PbBenchInner { Id = msg.Nested!.Id, Name = msg.Nested.Name },
            IntList = msg.IntList,
            StringList = msg.StringList,
            MessageList = msg.MessageList.ConvertAll(m => new PbBenchInner { Id = m.Id, Name = m.Name }),
            Map = msg.Map
        };

        // 预热 protobuf-net 类型模型
        var warmupMs = new MemoryStream();
        Serializer.Serialize(warmupMs, pbMsg);
        Serializer.Deserialize<PbBenchMessage>(new MemoryStream(warmupMs.ToArray()));

        using var ms = new System.IO.MemoryStream();
        Serializer.Serialize(ms, pbMsg);
        _pbnetData = ms.ToArray();
    }

    [Benchmark(Description = "PbLite Deserialize")]
    public BenchMessage PbLiteDeserialize()
    {
        var reader = new ProtoReader(new ReadOnlySequence<byte>(_pbliteData));
        return (BenchMessage)BenchMessageSerializer.Instance.Deserialize(ref reader, null);
    }

    [Benchmark(Description = "protobuf-net Deserialize")]
    public PbBenchMessage ProtobufNetDeserialize()
    {
        using var ms = new System.IO.MemoryStream(_pbnetData);
        return Serializer.Deserialize<PbBenchMessage>(ms);
    }
}

// ─── GetSize Benchmark ───────────────────────────────────

[MemoryDiagnoser]
public class GetSizeBenchmarks
{
    private BenchMessage _msg = null!;

    [Params("small", "large")]
    public string Scenario { get; set; } = "small";

    [GlobalSetup]
    public void Setup()
    {
        _msg = new BenchMessage
        {
            Int32Field = -12345,
            Int64Field = -9876543210L,
            StringField = "Hello PbLite Benchmark",
            FloatField = 3.14f,
            DoubleField = 2.718281828459045,
            BoolField = true,
            Nested = new BenchInner { Id = 777, Name = "Nested" },
            IntList = { 1, 2, 3, 100, -50 },
            StringList = { "a", "bb", "ccc" },
            MessageList =
            {
                new BenchInner { Id = 1, Name = "first" },
                new BenchInner { Id = 2, Name = "second" }
            },
            Map = { [10] = "ten", [20] = "twenty" }
        };

        if (Scenario == "large")
        {
            for (int i = 0; i < 100; i++)
            {
                _msg.IntList.Add(i);
                _msg.StringList.Add($"item_{i}");
                _msg.MessageList.Add(new BenchInner { Id = i, Name = $"name_{i}" });
                _msg.Map[i] = $"val_{i}";
            }
        }
    }

    [Benchmark(Description = "PbLite GetSize")]
    public int PbLiteGetSize()
    {
        return BenchMessageSerializer.Instance.GetSize(_msg);
    }
}

public static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
