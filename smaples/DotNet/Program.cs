using System.Buffers;
using PbLite;

// Register all generated serializers
PbLiteGeneratedSerializers.Register();

var person = new Person { Id = 42, Name = "Alice" };

// --- Explicit serializer path ---
var writer = new ArrayBufferWriter<byte>();
PersonSerializer.Instance.Serialize(writer, person);

Console.WriteLine($"Bytes: {writer.WrittenCount}");
Console.WriteLine($"Hex: {Convert.ToHexString(writer.WrittenSpan)}");

// --- Deserialize via explicit serializer ---
var reader = new ProtoReader(new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray()));
var restored = (Person)PersonSerializer.Instance.Deserialize(ref reader, null);

Console.WriteLine($"Restored: Id={restored.Id}, Name={restored.Name}");

// --- Global registry path ---
var serializer = SerializerRegistry.Get(typeof(Person))!;
var writer2 = new ArrayBufferWriter<byte>();
serializer.Serialize(writer2, person);

Console.WriteLine($"Global Bytes: {writer2.WrittenCount}");

[PbContract]
public class Person
{
    [PbMember(1)] public int Id { get; set; }
    [PbMember(2)] public string Name { get; set; } = "";
}
