using System;

namespace PbLite
{
    public enum PbWire
    {
        Varint = 0,
        ZigZag = 1,
        Fixed32 = 2,
        Fixed64 = 3,
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class PbContractAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class PbMemberAttribute : Attribute
    {
        public int Order { get; }
        public PbWire Wire { get; set; } = PbWire.Varint;

        public PbMemberAttribute(int order)
        {
            Order = order;
        }
    }
}
