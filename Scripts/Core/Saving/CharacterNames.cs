using System.Linq;

namespace Hoellenspiralenspiel.Scripts.Core.Saving;

public static class CharacterNames
{
    public const int    MaxLength = 16;
    public const string Fallback  = "Nameless";

    public static string Clean(string input)
    {
        var name = new string((input ?? string.Empty).Where(letter => !char.IsControl(letter)).ToArray()).Trim();

        if (name.Length > MaxLength)
            name = name[..MaxLength].TrimEnd();

        return name.Length == 0 ? Fallback : name;
    }
}
