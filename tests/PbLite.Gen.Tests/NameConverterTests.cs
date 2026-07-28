namespace PbLite.Gen.Tests;

public class NameConverterTests
{
    [Theory]
    [InlineData("player_id", "PlayerId")]
    [InlineData("msg_type", "MsgType")]
    [InlineData("content", "Content")]
    [InlineData("id", "Id")]
    [InlineData("a_b_c", "ABC")]
    [InlineData("already_pascal", "AlreadyPascal")]
    [InlineData("trailing_", "Trailing")]
    [InlineData("_leading", "Leading")]
    [InlineData("", "")]
    public void ToPascalCase_SnakeCase(string input, string expected)
    {
        Assert.Equal(expected, NameConverter.ToPascalCase(input));
    }

    [Theory]
    [InlineData("SCREAMING_SNAKE", "ScreamingSnake")]
    [InlineData("ALLCAPS", "ALLCAPS")]
    [InlineData("PascalCase", "PascalCase")]
    public void ToPascalCase_MiscCases(string input, string expected)
    {
        Assert.Equal(expected, NameConverter.ToPascalCase(input));
    }

    [Theory]
    [InlineData("UNKNOWN", "Unknown")]
    [InlineData("PLAYER_UNSPECIFIED", "PlayerUnspecified")]
    [InlineData("ACTIVE", "Active")]
    [InlineData("ONLINE", "Online")]
    public void EnumValueToPascalCase_AllCaps(string input, string expected)
    {
        Assert.Equal(expected, NameConverter.EnumValueToPascalCase(input));
    }

    [Theory]
    [InlineData("foo.bar", "Foo.Bar")]
    [InlineData("com.example.game", "Com.Example.Game")]
    [InlineData("", "")]
    [InlineData("single", "Single")]
    public void PackageToNamespace(string input, string expected)
    {
        Assert.Equal(expected, NameConverter.PackageToNamespace(input));
    }
}
