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
        /// <typeparam name="TWriter"></typeparam>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        int Serialize<TWriter>(TWriter writer, object value) where TWriter : IBufferWriter<byte>;

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        object Deserialize(ref ProtoReader reader, object? value);
    }
}