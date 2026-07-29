using System;
using System.Collections.Generic;

namespace PbLite
{
    public static class SerializerRegistry
    {
        private static readonly Dictionary<Type, IProtoSerializer> _serializers = new Dictionary<Type, IProtoSerializer>();

        public static IReadOnlyDictionary<Type, IProtoSerializer> All => _serializers;

        public static void Register(IProtoSerializer serializer)
        {
            _serializers[serializer.Type] = serializer;
        }

        public static IProtoSerializer? Get(Type type)
        {
            _serializers.TryGetValue(type, out var s);
            return s;
        }
    }
}