using PbLite;

[PbContract]
public class PlayerInfo
{
    [PbMember(1)] public int Id { get; set; }
    [PbMember(2)] public string Name { get; set; } = "";
}
