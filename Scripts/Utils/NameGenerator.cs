using System;

namespace Hoellenspiralenspiel.Scripts.Utils;

public static class NameGenerator
{
    private static readonly Random Rng = new();
    private static string[] worte1 =
    [
        "Dire",
        "Malevolent",
        "Grim",
        "Storm",
        "Rune",
        "Shadow",
        "Glyph",
        "Hailstone",
        "Demon",
        "Beast",
        "Stone",
        "Imp"
    ];
    private static string[] worte2 =
    [
        "Scratch",
        "Saw",
        "Cleaver",
        "Fang",
        "Bite",
        "Bludgeon",
        "Stinger",
        "Thirst",
        "Hate",
        "Bargain",
        "Strike",
        "Tooth"
    ];

    public static string GenerateRare()
    {
        var ersterTeil  = worte1[Rng.Next(0, worte1.Length)];
        var zweiterTeil = worte2[Rng.Next(0, worte2.Length)];

        return $"{ersterTeil} {zweiterTeil}";
    }
}