using System;

namespace PbLite
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class PbContractAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class PbMemberAttribute : Attribute
    {
        public int Order { get; }

        public PbMemberAttribute(int order)
        {
            Order = order;
        }
    }
}
