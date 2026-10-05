using System.Collections.Generic;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Items;

namespace Hoellenspiralenspiel.Scripts.Core.Saving;

public sealed class SaveGame
{
    public const int CurrentVersion = 7;

    public int                    Version   { get; set; } = CurrentVersion;
    public CharacterSave          Character { get; set; } = new();
    public List<string>           Loadout   { get; set; } = new();

    //Fehlt vor Version 4. Parallel zu Loadout je Platz die Item-Basis eines Tranks oder null
    public List<string>           LoadoutConsumables { get; set; } = new();

    public List<PlacedItemSave>   Inventory { get; set; } = new();
    public List<EquippedItemSave> Equipment { get; set; } = new();
    public List<ItemSave>         Unplaced  { get; set; } = new();
    public List<PlacedItemSave>   Stash     { get; set; } = new();
    public JourneySave            Journey   { get; set; }

    //Fehlt in Spielständen vor Version 3. Der Händler würfelt dann beim Laden einen Bestand
    public VendorSave             Vendor    { get; set; }

    //Nur in Spielständen der Version 1. Seit Version 2 steht der Abstieg je Kreis unter Journey
    public DescentSave            Descent   { get; set; }
}

public sealed class DescentSave
{
    public int                     Seed   { get; set; }
    public int                     Depth  { get; set; }
    public List<ExploredLevelSave> Levels { get; set; } = new();
}

public sealed class ExploredLevelSave
{
    //Seit Version 6 der Ort als Text, etwa "f2" oder "f2/d0/l1". Fehlt er, gilt die Tiefe: Tiefe N ist die Fläche N
    public string    Location { get; set; } = string.Empty;
    public int       Depth    { get; set; }
    public string    Revealed { get; set; } = string.Empty;
    public List<int> Killed   { get; set; } = new();
}

public sealed class JourneySave
{
    public int              UnlockedCircles { get; set; } = 1;
    public List<CircleSave> Circles         { get; set; } = new();
    public TownPortalSave   TownPortal      { get; set; }
}

public sealed class CircleSave
{
    public string                  CircleId     { get; set; } = string.Empty;
    public int                     Seed         { get; set; }
    public int                     DeepestDepth { get; set; }

    //Fehlt vor Version 5 und steht dann auf 0: Der Stand des Kreises, mit dem die Ebenen entstanden sind
    public int                     ContentVersion { get; set; }

    public List<ExploredLevelSave> Levels       { get; set; } = new();
}

public sealed class TownPortalSave
{
    public string CircleId { get; set; } = string.Empty;
    public int    Depth    { get; set; }
    public float  X        { get; set; }
    public float  Z        { get; set; }
}

public sealed class CharacterSave
{
    public string Name            { get; set; } = string.Empty;
    public int    Level           { get; set; } = 1;
    public long   XpTotal         { get; set; }
    public int    AttributePoints { get; set; }
    public int    Gold            { get; set; }
    public int    StashGold       { get; set; }
    public int    Strength        { get; set; } = 1;
    public int    Dexterity       { get; set; } = 1;
    public int    Intelligence    { get; set; } = 1;
    public int    Constitution    { get; set; } = 1;
    public int    Awareness       { get; set; } = 1;
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

    //Fehlt vor Version 7. Das Y eines Affixes "Adds X to Y", sonst 0
    public float            ValueTo      { get; set; }
}

public sealed class VendorSave
{
    public int                  ItemLevel { get; set; } = 1;
    public List<PlacedItemSave> Stock     { get; set; } = new();
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
