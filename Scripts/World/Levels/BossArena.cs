using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Levels;
using Hoellenspiralenspiel.Scripts.Environment;
using Hoellenspiralenspiel.Scripts.Objects;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.Units.Enemies;

namespace Hoellenspiralenspiel.Scripts.World.Levels;

//Der Raum des Bosses, auf der letzten Ebene an der Stelle des Ausgangs. Seine Gitter schließen sich, solange der Held lebend
//darin steht und der Boss lebt. Fällt der Boss, öffnen sie sich, und an PortalSpot erscheint das Portal zurück in den Hub
public partial class BossArena : RoomTemplate
{
    private const string GateScenePath   = "res://Scenes/Objects/boss_gate.tscn";
    private const string PortalScenePath = "res://Scenes/Objects/boss_portal.tscn";
    private const string PortalSpotName  = "PortalSpot";

    private readonly List<BossGate> gates = new();

    private Enemy      boss;
    private Hero       hero;
    private bool       isSealed;
    private BossPortal portal;
    private RoomZone   zone;

    public bool IsSealed => isSealed;

    public bool HasPortal => IsInstanceValid(portal);

    public IReadOnlyList<BossGate> Gates => gates;

    public override void _Ready()
        => zone = GetNodeOrNull<RoomZone>(nameof(RoomZone));

    //Ein Gitter an jeder Tür, die der Grundriss benutzt. Es steht offen, damit die Wegfindung durch die Tür führt
    public void BuildGates(PlacedRoom placed, LevelLayout layout, LevelGrid grid)
    {
        var scene = GD.Load<PackedScene>(GateScenePath);

        foreach (var door in placed.Doors)
        {
            if (!layout.IsOpening(door.Inside, door.Outside))
                continue;

            var gate = scene.Instantiate<BossGate>();

            gate.Name = $"Gate{door.Side}{gates.Count}";

            AddChild(gate);

            gate.GlobalPosition = (grid.GetCenter(door.Inside) + grid.GetCenter(door.Outside)) / 2f;
            gate.GlobalBasis    = door.Side.IsAlongX() ? Basis.Identity : new Basis(Vector3.Up, Mathf.Pi / 2f);

            gates.Add(gate);
        }
    }

    //Ohne Boss bleibt der Raum offen, etwa wenn der Boss bei einem früheren Besuch gefallen ist
    public void Arm(Enemy watchedBoss, Hero player)
    {
        boss = watchedBoss;
        hero = player;
    }

    public override void _PhysicsProcess(double delta)
    {
        var bossAlive  = boss is not null && IsInstanceValid(boss) && boss.IsInsideTree() && !boss.IsDying && !boss.IsDead;
        var heroAlive  = hero is not null && IsInstanceValid(hero) && !hero.IsDead;
        var heroInRoom = zone is not null && WallFade.RoomOfHero == zone.Id;
        var sealedNow  = BossArenaRule.IsSealed(heroInRoom, heroAlive, bossAlive);

        if (sealedNow == isSealed)
            return;

        isSealed = sealedNow;

        foreach (var gate in gates)
            gate.SetClosed(isSealed);
    }

    //Das Portal zurück in den Hub. Wohin es führt, entscheidet der Abstieg
    public BossPortal ShowPortal()
    {
        if (HasPortal)
            return portal;

        portal = GD.Load<PackedScene>(PortalScenePath).Instantiate<BossPortal>();

        AddChild(portal);

        if (GetNodeOrNull<Node3D>(PortalSpotName) is { } spot)
            portal.GlobalTransform = spot.GlobalTransform;

        return portal;
    }
}
