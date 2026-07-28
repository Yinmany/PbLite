namespace PbLite
{
    public enum WireType
    {
        Varint = 0,
        Fixed64 = 1,
        SFixed64 = 1,
        Double = 1,

        LengthDelimited = 2,
        StartGroup = 3,
        EndGroup = 4,

        Fixed32 = 5,
        SFixed32 = 5,
        Float = 5,
    }

    public static class ProtoWire
    {
        public static uint MakeTag(int fieldNumber, WireType wireType)
        {
            return (uint)((fieldNumber << 3) | (int)wireType);
        }

        public static int GetFieldNumber(uint tag)
        {
            return (int)(tag >> 3);
        }

        public static WireType GetWireType(uint tag)
        {
            return (WireType)(tag & 7);
        }
    }
}