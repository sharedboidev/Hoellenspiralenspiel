using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using Hoellenspiralenspiel.Scripts.Core.Economy;
using Hoellenspiralenspiel.Scripts.UI;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.World;

namespace Hoellenspiralenspiel.Scripts.Objects;

//Gold am Boden. Der Held hebt es auf, sobald er nah genug herankommt
public partial class CoinPile : Node3D
{
    private const float HopMeters      = 0.35f;
    private const int   PickupFontSize = 28;

    private static readonly PackedScene Scene = ResourceLoader.Load<PackedScene>("res://Scenes/Objects/coin_pile.tscn");

    private static readonly Color PickupColor = new(1f, 0.84f, 0.3f);

    private static readonly List<CoinPile> LyingPiles = new();

    private Hero   collector;
    private double secUntilCollectable;

    //In Pixeln wie alle Reichweiten, ab dem Körperrand des Helden
    [Export]
    public float PickupRadius { get; set; } = 110f;

    //Gold, das so nah an einem Haufen fällt, landet auf ihm
    [Export]
    public float MergeRadius { get; set; } = 120f;

    //So lange bleibt frisch gefallenes Gold liegen, damit man es fallen sieht
    [Export]
    public double PickupDelaySec { get; set; } = 0.35;

    //Ab welchem Betrag welche Stufe zu sehen ist: eine bis fünf lose Münzen, ein bis drei Stapel, fünf Stapel
    [Export]
    public Array<int> TierThresholds { get; set; } = new(CoinPileTiers.DefaultThresholds);

    //Jedes Kind ist das Aussehen einer Stufe, in der Reihenfolge der Schwellen
    [Export]
    public Node3D Tiers { get; set; }

    public static IReadOnlyList<CoinPile> Lying => LyingPiles;

    public int Amount { get; private set; }

    public int Tier { get; private set; } = -1;

    public static CoinPile DropAround(Node3D parent, Vector3 center, int amount, Hero collector)
    {
        if (amount <= 0)
            return null;

        var onGround = WorldScale.OnGround(center);
        var nearby   = LyingPiles.FirstOrDefault(pile => !pile.IsQueuedForDeletion() && WorldScale.GroundDistancePx(pile.GlobalPosition, onGround) <= pile.MergeRadius);

        if (nearby is not null)
        {
            nearby.Add(amount);

            return nearby;
        }

        var pile = Scene.Instantiate<CoinPile>();

        pile.collector = collector;

        parent.AddChild(pile);

        pile.GlobalPosition = Lootbag.FindFreeSpot(parent, onGround, 0f);

        pile.Add(amount);

        return pile;
    }

    public override void _EnterTree()
        => LyingPiles.Add(this);

    public override void _ExitTree()
        => LyingPiles.Remove(this);

    public override void _PhysicsProcess(double delta)
    {
        if (secUntilCollectable > 0)
        {
            secUntilCollectable -= delta;

            return;
        }

        if (IsInstanceValid(collector) && !collector.IsDead && collector.DistancePxTo(GlobalPosition) <= PickupRadius)
            Collect();
    }

    public void Add(int amount)
    {
        if (amount <= 0)
            return;

        Amount = (int)System.Math.Min(int.MaxValue, (long)Amount + amount);

        secUntilCollectable = PickupDelaySec;

        ShowTier();
        Hop();
    }

    public void Collect()
    {
        if (IsQueuedForDeletion() || !IsInstanceValid(collector))
            return;

        collector.Gold.Add(Amount);

        CombatText.Show(collector, $"+{Amount:N0} Gold", PickupColor, PickupFontSize);

        QueueFree();
    }

    private void ShowTier()
    {
        Tier = Mathf.Max(0, CoinPileTiers.GetTier(Amount, TierThresholds.ToList()));

        if (Tiers is null)
            return;

        var looks = Tiers.GetChildren().OfType<Node3D>().ToList();
        var shown = Mathf.Min(Tier, looks.Count - 1);

        for (var i = 0; i < looks.Count; i++)
            looks[i].Visible = i == shown;
    }

    private void Hop()
    {
        if (Tiers is null)
            return;

        var tween = CreateTween();

        tween.TweenProperty(Tiers, "position", Vector3.Up * HopMeters, 0.1f);
        tween.TweenProperty(Tiers, "position", Vector3.Zero, 0.15f);
    }
}
