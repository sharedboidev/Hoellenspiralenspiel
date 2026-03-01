using System;
using System.Linq;

namespace Hoellenspiralenspiel.Scripts.Utils;

public static class NameGenerator
{
    private static readonly Random   Rng                = new();
    private static readonly string[] WorteDemons        = ["Imp", "Demon","Cerebus","Valkyre"];
    private static readonly string[] WorteWitterungen   = ["Gale", "Storm", "Hailstone", "Stone",];
    private static readonly string[] WorteAnimals       = ["Raven", "Snake", "Beast","Wing","Eagle"];
    private static readonly string[] WorteCruelStuff    = ["Pain", "Grim", "Shadow", "Satanic", "Dire", "Malevolent", "Havoc", "Brimstone",];
    private static readonly string[] WorteSymbols       = ["Rune", "Glyph",];
    private static readonly string[] WorteArmors        = ["Suit"];
    private static readonly string[] WorteKlingenWaffen = ["Scratch", "Saw", "Cleaver", "Fang", "Bite", "Bludgeon", "Stinger", "Thirst", "Hate", "Bargain", "Strike", "Tooth"];
    private static readonly string[] WorteHelme         = ["Hood", "Brow", "Cowl", "Visor", "Mask","Head","Visage","Crest","Casque"];

    public static string GenerateRareWeapon()
    {
        var worteWaffen1 = WorteDemons.Union(WorteWitterungen).Union(WorteCruelStuff).Union(WorteSymbols).ToArray();
        var ersterTeil   = worteWaffen1[Rng.Next(0, worteWaffen1.Length)];
        var zweiterTeil  = WorteKlingenWaffen[Rng.Next(0, WorteKlingenWaffen.Length)];

        return $"{ersterTeil} {zweiterTeil}";
    }

    public static string GenerateRareArmor()
    {
        var worteWaffen1 = WorteDemons.Union(WorteWitterungen).Union(WorteCruelStuff).Union(WorteSymbols).Union(WorteAnimals).ToArray();
        var ersterTeil   = worteWaffen1[Rng.Next(0, worteWaffen1.Length)];
        var zweiterTeil  = WorteHelme[Rng.Next(0, WorteHelme.Length)];

        return $"{ersterTeil} {zweiterTeil}";
    }
}