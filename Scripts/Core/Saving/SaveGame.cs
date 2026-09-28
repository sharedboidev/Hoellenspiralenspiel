using System.Collections.Generic;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Items;

namespace Hoellenspiralenspiel.Scripts.Core.Saving;

public sealed class SaveGame
{
    public const int CurrentVersion = 1;

    public int                    Version   { get; set; } = CurrentVersion;
    public CharacterSave          Character { get; set; } = new();
    public List<string>           Loadout   { get; set; } = new();
    public List<PlacedItemSave>   Inventory { get; set; } = new();
    public List<EquippedItemSave> Equipment { get; set; } = new();
    public List<ItemSave>         Unplaced  { get; set; } = new();
}

public sealed class CharacterSave
{
    public int  Level           { get; set; } = 1;
    public long XpTotal         { get; set; }
    public int  AttributePoints { get; set; }
    public int  Strength        { get; set; } = 1;
    public int  Dexterity       { get; set; } = 1;
    public int  Intelligence    { get; set; } = 1;
    public int  Constitution    { get; set; } = 1;
    public int  Awareness       { get; set; } = 1;
}

public sealed class ItemSave
{
    public string          BaseId    { get; set; } = string.Empty;
    public int             ItemLevel { get; set; } = 1;
    public int             StackSize { get; set; } = 1;
    public string          RareName  { get; set; }
    public List<AffixSave> Affixes   { get; set; } = new();
}

public sealed class AffixSave
{
    public AffixType        Type         { get; set; }
    public CombatStat       Stat         { get; set; }
    public ModificationType Modification { get; set; }
    public float            Value        { get; set; }
    public string           NameAddition { get; set; }
    public bool             IsLocal      { get; set; }
}

public sealed class PlacedItemSave
{
    public int      X    { get; set; }
    public int      Y    { get; set; }
    public ItemSave Item { get; set; } = new();
}

public sealed class EquippedItemSave
{
    public ItemSlot Place { get; set; }
    public ItemSave Item  { get; set; } = new();
}
