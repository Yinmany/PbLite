using System;
using System.Buffers;
using PbLite;
using UnityEngine;

public class PbLiteSample : MonoBehaviour
{
    void Start()
    {
        // Register all generated serializers
        PbLiteGeneratedSerializers.Register();

        var player = new PlayerInfo { Id = 42, Name = "Alice" };

        // --- Explicit serializer ---
        var writer = new ArrayBufferWriter<byte>();
        PlayerInfoSerializer.Instance.Serialize(writer, player);

        Debug.Log($"Bytes: {writer.WrittenCount}");
        Debug.Log($"Hex: {BitConverter.ToString(writer.WrittenSpan.ToArray())}");

        var reader = new ProtoReader(new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray()));
        var restored = (PlayerInfo)PlayerInfoSerializer.Instance.Deserialize(ref reader, null);
        Debug.Log($"Restored: Id={restored.Id}, Name={restored.Name}");

        // --- Global registry ---
        var globalSerializer = SerializerRegistry.Get(typeof(PlayerInfo))!;
        var writer2 = new ArrayBufferWriter<byte>();
        globalSerializer.Serialize(writer2, player);
        Debug.Log($"Global bytes: {writer2.WrittenCount}");
    }
}
