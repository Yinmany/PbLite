using System.Text;

namespace PbLite.ProtoGen;

static class NameConverter
{
    /// <summary>
    /// Convert snake_case / SCREAMING_SNAKE_CASE to PascalCase.
    /// Names without underscores get first letter capitalized.
    /// </summary>
    public static string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        if (name.Contains('_'))
        {
            var sb = new StringBuilder(name.Length);
            bool nextUpper = true;
            foreach (char c in name)
            {
                if (c == '_') { nextUpper = true; continue; }
                sb.Append(nextUpper ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c));
                nextUpper = false;
            }
            return sb.ToString();
        }

        return char.ToUpperInvariant(name[0]) + name[1..];
    }

    /// <summary>
    /// Convert enum value names. Handles SCREAMING_SNAKE_CASE and ALLCAPS.
    /// </summary>
    public static string EnumValueToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        if (name.Contains('_'))
            return ToPascalCase(name);

        if (IsAllUpper(name) && name.Length > 1)
            return char.ToUpperInvariant(name[0]) + name[1..].ToLowerInvariant();

        return char.ToUpperInvariant(name[0]) + name[1..];
    }

    private static bool IsAllUpper(string s)
    {
        foreach (char c in s)
            if (char.IsLetter(c) && !char.IsUpper(c))
                return false;
        return true;
    }

    /// <summary>
    /// proto package "foo.bar" → C# namespace "Foo.Bar"
    /// </summary>
    public static string PackageToNamespace(string package)
    {
        if (string.IsNullOrEmpty(package)) return "";
        var parts = package.Split('.');
        for (int i = 0; i < parts.Length; i++)
            parts[i] = ToPascalCase(parts[i]);
        return string.Join('.', parts);
    }
}
