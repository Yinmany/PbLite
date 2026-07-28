using System;
using System.Buffers;

namespace PbLite
{
    /// <summary>
    /// 序列化器接口
    /// </summary>
    public interface IProtoSerializer
    {
        /// <summary>
        /// 要序列化的目标Type
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// 序列化
        /// </summary>
        void Serialize<TWriter>(TWriter writer, object value) where TWriter : IBufferWriter<byte>;

        /// <summary>
        /// 计算序列化后的字节大小（不含外层 tag + length）
        /// </summary>
        int GetSize(object value);

        /// <summary>
        /// 反序列化
        /// </summary>
        object Deserialize(ref ProtoReader reader, object? value);
    }
}
