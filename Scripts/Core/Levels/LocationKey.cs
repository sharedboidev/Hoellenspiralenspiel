using System;
using System.Globalization;

namespace Hoellenspiralenspiel.Scripts.Core.Levels;

//Ein Ort im Abstieg: eine Fläche, ab Etappe 5 von M8 auch eine Ebene im Dungeon einer Fläche. Als Text "f2" oder "f2/d0/l1".
//Die Ebenen der Kreise ohne Flächen zählen wie Flächen, Tiefe N ist "fN"
public readonly record struct LocationKey
{
    public const int NoDungeon = -1;

    //Um eins verschoben, damit auch default(LocationKey) keinen Dungeon nennt
    private readonly int dungeonPlusOne;

    public LocationKey(int field, int dungeon = NoDungeon, int dungeonLevel = 0)
    {
        Field          = field;
        dungeonPlusOne = Math.Max(NoDungeon, dungeon) + 1;
        DungeonLevel   = dungeonPlusOne == 0 ? 0 : Math.Max(1, dungeonLevel);
    }

    public int Field { get; }

    public int Dungeon => dungeonPlusOne - 1;

    public int DungeonLevel { get; }

    public bool IsInDungeon => Dungeon != NoDungeon;

    //Tiefe 0 heißt, der Held ist nicht im Kreis
    public bool IsValid => Field >= 1;

    public static LocationKey Of(int field)
        => new(field);

    public LocationKey InDungeon(int dungeon, int level)
        => new(Field, dungeon, level);

    public override string ToString()
        => IsInDungeon ? $"f{Field}/d{Dungeon}/l{DungeonLevel}" : $"f{Field}";

    public static bool TryParse(string text, out LocationKey key)
    {
        key = default;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        var parts = text.Split('/');

        if (parts.Length is not (1 or 3) || !TryReadPart(parts[0], 'f', out var field))
            return false;

        if (parts.Length == 1)
        {
            key = new LocationKey(field);

            return true;
        }

        if (!TryReadPart(parts[1], 'd', out var dungeon) || !TryReadPart(parts[2], 'l', out var level) || level < 1)
            return false;

        key = new LocationKey(field, dungeon, level);

        return true;
    }

    private static bool TryReadPart(string part, char prefix, out int value)
    {
        value = 0;

        return part.Length > 1 && part[0] == prefix && int.TryParse(part.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out value);
    }
}
