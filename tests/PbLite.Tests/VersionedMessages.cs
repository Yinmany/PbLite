using PbLite;

namespace PbLite.Tests
{
    [PbContract]
    public class V1Message
    {
        [PbMember(1)] public int Id { get; set; }
        [PbMember(2)] public string Name { get; set; } = "";
    }

    [PbContract]
    public class V2Message
    {
        [PbMember(1)] public int Id { get; set; }
        [PbMember(2)] public string Name { get; set; } = "";
        [PbMember(3)] public int NewField { get; set; } = 42;
        [PbMember(4)] public string Extra { get; set; } = "default";
    }
}
