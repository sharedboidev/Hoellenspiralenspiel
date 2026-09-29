using System.Collections.Generic;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Resources.Enemies;
using Hoellenspiralenspiel.Resources.MonsterMods;
using Hoellenspiralenspiel.Scripts.Core.Enemies;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Core.Spatial;
using Hoellenspiralenspiel.Scripts.Enemies;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Objects;
using Hoellenspiralenspiel.Scripts.Skills;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.Units.Enemies;
using Hoellenspiralenspiel.Scripts.World;

namespace Hoellenspiralenspiel.Scripts.Controllers;

//Die Mitte ist der Wunschplatz. Ist er belegt oder soll die Gruppe streuen, liegt der Platz im Umkreis.
//HeroClearancePx hält Abstand zum Helden, damit eine Gruppe nicht schon beim Laden der Karte angreift
public readonly record struct SpawnArea(Vector3 Center, float ScatterPx = 0f, float GapPx = 0f, float HeroClearancePx = 0f);

public partial class EnemyController : Node
{
    private const float SightHeightMeters = 0.5f;
    private const float ProbeLiftMeters   = 0.1f;
    private const float AggroMarginPx     = 100f;

    private readonly List<Enemy>    enemies       = new();
    private readonly List<BaseUnit> unitsNearSpot = new();

    [Export]
    public Hero Hero { get; set; }

    [Export]
    public Node3D EnemyContainer { get; set; }

    [Export]
    public Node SpawnMarkers { get; set; }

    [Export]
    public Lootsystem Lootsystem { get; set; }

    //Jedes Monster der Karte hat dieses Level, plus die Anpassungen an Monster und Spawn-Marker
    [Export]
    public int AreaLevel { get; set; } = 1;

    //Ruhende Monster außerhalb dieses Abstands zum Helden denken und bewegen sich nicht
    [Export]
    public float SimulationRadius { get; set; } = 2500f;

    [ExportGroup("Elite")]
    [Export(PropertyHint.Range, "0,100,0.1")]
    public float EliteChancePercent { get; set; } = 10f;

    [Export]
    public float EliteScale { get; set; } = 1.25f;

    [Export]
    public float EliteXpFactor { get; set; } = 1.5f;

    [Export]
    public int EliteLootRolls { get; set; } = 2;

    [Export]
    public Color EliteNameColor { get; set; } = new(0.45f, 0.68f, 1f);

    [ExportGroup("Rare Elite")]
    [Export(PropertyHint.Range, "0,100,0.1")]
    public float RareEliteChancePercent { get; set; } = 4f;

    [Export]
    public float RareEliteScale { get; set; } = 1.5f;

    [Export]
    public float RareEliteXpFactor { get; set; } = 3f;

    [Export]
    public int RareEliteLootRolls { get; set; } = 3;

    [Export]
    public Color RareEliteNameColor { get; set; } = new(1f, 0.82f, 0.25f);

    public IReadOnlyList<Enemy> Enemies => enemies;

    public override void _Ready()
    {
        if (SpawnMarkers is null)
            return;

        foreach (var marker in SpawnMarkers.GetAllChildren<SpawnMarker>())
            SpawnGroupAt(marker);
    }

    public override void _PhysicsProcess(double delta)
    {
        var heroPosition = Hero.GlobalPosition;

        //Über den Index, weil ein Monster beim Denken weitere beschwören kann
        for (var i = 0; i < enemies.Count; i++)
        {
            var enemy   = enemies[i];
            var isAwake = !enemy.IsResting || WorldScale.GroundDistancePx(enemy.GlobalPosition, heroPosition) <= SimulationRadius;

            enemy.SetAwake(isAwake);

            if (isAwake)
                enemy.Think(delta);
        }
    }

    private void SpawnGroupAt(SpawnMarker marker)
    {
        if (marker.Enemy is null)
        {
            GD.PushWarning($"Der Spawn-Marker {marker.Name} hat keinen Gegner.");

            return;
        }

        var level   = EnemyScaling.GetLevel(AreaLevel, marker.Enemy.LevelOffset, marker.LevelOffset);
        var chances = new EnemyRarityChances(EliteChancePercent, RareEliteChancePercent);

        for (var i = 0; i < marker.AmountToSpawn; i++)
        {
            var modCount = EnemyRarityRules.RollModCount(chances, GameRandom.Shared);

            Spawn(marker.Enemy, new SpawnArea(marker.GlobalPosition, marker.ScatterRadius, marker.MinGap, marker.Enemy.AggroRange + AggroMarginPx), marker.Name, level, modCount);
        }
    }

    public Enemy Spawn(EnemyResource definition, Vector3 position, string spawnGroup, int level, int modCount = 0)
        => Spawn(definition, new SpawnArea(position), spawnGroup, level, modCount);

    public Enemy Spawn(EnemyResource definition, SpawnArea area, string spawnGroup, int level, int modCount = 0)
    {
        var traits = new MonsterTraits(level, definition.UsesProjectiles);
        var mods   = MonsterModRoller.Pick(MonsterModLibrary.Pool, modCount, traits, GameRandom.Shared)
                                     .Select(mod => MonsterModLibrary.Find(mod.Id))
                                     .ToList();

        return Spawn(definition, area, spawnGroup, level, mods);
    }

    public Enemy Spawn(EnemyResource definition, Vector3 position, string spawnGroup, int level, IReadOnlyList<MonsterModResource> mods)
        => Spawn(definition, new SpawnArea(position), spawnGroup, level, mods);

    public Enemy Spawn(EnemyResource definition, SpawnArea area, string spawnGroup, int level, IReadOnlyList<MonsterModResource> mods)
    {
        var enemy = definition.Scene.Instantiate<Enemy>();
        var look  = GetLookOf(EnemyRarityRules.FromModCount(mods.Count));

        enemy.Configure(definition, level, mods, look);

        enemy.Position   = FindFreeSpot(area, enemy.GetBodyRadius() * look.Scale);
        enemy.SpawnGroup = spawnGroup;
        enemy.Controller = this;
        enemy.Target     = Hero;
        enemy.Provoked   += CallGroupToArms;
        enemy.Died       += OnEnemyDied;

        enemies.Add(enemy);
        EnemyContainer.AddChild(enemy);

        return enemy;
    }

    //Frei ist ein Platz ohne Mauer und ohne anderen Körper, den man von der Mitte aus sieht
    public Vector3 FindFreeSpot(SpawnArea area, float bodyRadius, BaseUnit ignored = null)
    {
        var center   = WorldScale.OnGround(area.Center);
        var space    = EnemyContainer.GetWorld3D().DirectSpaceState;
        var probe    = new SphereShape3D { Radius = bodyRadius };
        var wasFound = SpotSearch.TryFind(new Spot(WorldScale.ToPx(center.X), WorldScale.ToPx(center.Z)),
                                          area.ScatterPx,
                                          GameRandom.Shared,
                                          spot => IsFree(ToWorld(spot), center, bodyRadius, area, ignored, space, probe),
                                          out var found);

        if (!wasFound)
            GD.PushWarning($"Um {center} ist kein Platz frei, der Körper landet auf der Mitte.");

        return ToWorld(found);
    }

    private static Vector3 ToWorld(Spot spot)
        => new(WorldScale.ToMeters(spot.X), 0f, WorldScale.ToMeters(spot.Y));

    private bool IsFree(Vector3 point, Vector3 center, float bodyRadius, SpawnArea area, BaseUnit ignored, PhysicsDirectSpaceState3D space, Shape3D probe)
    {
        var reachPx = WorldScale.ToPx(bodyRadius) + area.GapPx;

        if (area.HeroClearancePx > 0f && IsInstanceValid(Hero) && Hero.DistancePxTo(point) < WorldScale.ToPx(bodyRadius) + area.HeroClearancePx)
            return false;

        UnitRegistry.FindNear(point, reachPx, unitsNearSpot);

        foreach (var unit in unitsNearSpot)
        {
            if (unit != ignored && unit.IsSolid && unit.DistancePxTo(point) < reachPx)
                return false;
        }

        var touch = new PhysicsShapeQueryParameters3D
        {
            Shape         = probe,
            Transform     = new Transform3D(Basis.Identity, point + Vector3.Up * (bodyRadius + ProbeLiftMeters)),
            CollisionMask = CollisionLayers.Walls
        };

        if (space.IntersectShape(touch, 1).Count > 0)
            return false;

        if (point.IsEqualApprox(center))
            return true;

        var sight = PhysicsRayQueryParameters3D.Create(center + Vector3.Up * SightHeightMeters, point + Vector3.Up * SightHeightMeters, CollisionLayers.Walls);

        return space.IntersectRay(sight).Count == 0;
    }

    private EnemyRarityLook GetLookOf(EnemyRarity rarity)
        => rarity switch
        {
            EnemyRarity.Elite     => new EnemyRarityLook(EliteScale, EliteXpFactor, EliteLootRolls, EliteNameColor),
            EnemyRarity.RareElite => new EnemyRarityLook(RareEliteScale, RareEliteXpFactor, RareEliteLootRolls, RareEliteNameColor),
            _                     => EnemyRarityLook.Normal
        };

    private void OnEnemyDied(BaseUnit unit)
    {
        if (unit is not Enemy enemy)
            return;

        enemies.Remove(enemy);

        CallGroupToArms(enemy);

        Hero.GainExperience(enemy.XpGranted);

        //Died feuert genau einmal pro Gegner, daher entsteht der Loot hier und nicht bei jeder Lebensänderung
        SpawnLootbags(enemy);
    }

    private void CallGroupToArms(Enemy caller)
    {
        foreach (var enemy in enemies)
        {
            if (enemy != caller && !enemy.IsInCombat && enemy.SpawnGroup == caller.SpawnGroup)
                enemy.Provoke();
        }
    }

    private void SpawnLootbags(Enemy enemy)
    {
        if (Lootsystem is null)
            return;

        var loot = Lootsystem.GenerateLoot(enemy);

        foreach (var item in loot)
            Lootbag.DropAround(Hero.GetParent<Node3D>(), enemy.GlobalPosition, 0f, item, Hero.Items);
    }
}
