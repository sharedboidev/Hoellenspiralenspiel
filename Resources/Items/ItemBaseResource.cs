using System;
using Godot;
using Godot.Collections;
using Hoellenspiralenspiel.Scripts.Core.Items;

namespace Hoellenspiralenspiel.Resources.Items;

[GlobalClass]
public abstract partial class ItemBaseResource : Resource
{
    [Export]
    public string Id { get; set; } = string.Empty;

    [Export]
    public string DisplayName { get; set; } = string.Empty;

    [Export]
    public Texture2D Icon { get; set; }

    [Export]
    public Vector2I SlotSize { get; set; } = Vector2I.One;

    [Export]
    public Dictionary<Requirement, int> Requirements { get; set; } = new();

    //Grundpreis in Gold für ein Stück ohne Affixe. 0 heißt unverkäuflich
    [Export(PropertyHint.Range, "0,10000,1,or_greater")]
    public int Price { get; set; }

    //Die Definition entsteht beim ersten Zugriff. Wer danach Werte der Resource ändert, sieht davon nichts
    public ItemDefinition Definition => field ??= CreateBaseDefinition() with
    {
        Width = Math.Max(1, SlotSize.X),
        Height = Math.Max(1, SlotSize.Y),
        Price = Math.Max(0, Price),
        Requirements = new System.Collections.Generic.Dictionary<Requirement, int>(Requirements)
    };

    protected abstract ItemDefinition CreateBaseDefinition();
}