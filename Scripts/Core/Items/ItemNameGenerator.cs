using System.Linq;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Rng;

namespace Hoellenspiralenspiel.Scripts.Core.Items;

public static class ItemNameGenerator
{
    private static readonly string[] Demons     = ["Imp", "Demon", "Cerebus", "Valkyre"];
    private static readonly string[] Weather    = ["Gale", "Storm", "Hailstone", "Stone"];
    private static readonly string[] Animals    = ["Raven", "Snake", "Beast", "Wing", "Eagle"];
    private static readonly string[] CruelStuff = ["Pain", "Grim", "Shadow", "Satanic", "Dire", "Malevolent", "Havoc", "Brimstone"];
    private static readonly string[] Symbols    = ["Rune", "Glyph"];
    private static readonly string[] Blades     = ["Scratch", "Saw", "Cleaver", "Fang", "Bite", "Bludgeon", "Stinger", "Thirst", "Hate", "Bargain", "Strike", "Tooth"];
    private static readonly string[] Helmets    = ["Hood", "Brow", "Cowl", "Visor", "Mask", "Head", "Visage", "Crest", "Casque"];
    private static readonly string[] Shields    = ["Guard", "Wall", "Bulwark", "Ward", "Aegis", "Rampart"];

    private static readonly string[] WeaponFirstWords = Demons.Union(Weather).Union(CruelStuff).Union(Symbols).ToArray();
    private static readonly string[] ArmorFirstWords  = WeaponFirstWords.Union(Animals).ToArray();

    public static string Generate(ItemDefinition item, IRandomSource random)
    {
        if (item.Kind == ItemKind.Weapon)
            return $"{Pick(WeaponFirstWords, random)} {Pick(Blades, random)}";

        return $"{Pick(ArmorFirstWords, random)} {Pick(item.Slot == ItemSlot.Offhand ? Shields : Helmets, random)}";
    }

    private static string Pick(string[] words, IRandomSource random)
        => words[random.NextInt(0, words.Length)];
}
