using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Resources.Enemies;
using Hoellenspiralenspiel.Resources.MonsterMods;
using Hoellenspiralenspiel.Scripts.Core.Economy;
using Hoellenspiralenspiel.Scripts.Core.Enemies;
using Hoellenspiralenspiel.Scripts.Core.Levels;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Core.Spatial;
using Hoellenspiralenspiel.Scripts.Enemies;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Objects;
using Hoellenspiralenspiel.Scripts.Skills;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.Units.Enemies;
using Hoellenspiralenspiel.Scripts.World;
using Hoellenspiralenspiel.Scripts.World.Levels;

namespace Hoellenspiralenspiel.Scripts.Controllers;

//Die Mitte ist der Wunschplatz. Ist er belegt oder soll die Gruppe streuen, liegt der Platz im Umkreis.
//HeroClearancePx hält Abstand zum Helden, damit eine Gruppe nicht schon beim Laden der Karte angreift
public readonly record struct SpawnArea(Vector3 Center, float ScatterPx = 0f, float GapPx = 0f, float HeroClearancePx = 0f);

public partial class EnemyController : Node
{
    private const float SightHeightMeters   = 0.5f;
    private const float ProbeLiftMeters     = 0.1f;
    private const float AggroMarginPx       = 100f;
    private const float EyeHeightMeters     = 1.5f;
    private const int   SightChecksPerFrame = 8;

    private readonly List<Node3D>   effects       = new();
    private readonly List<Enemy>    enemies       = new();
    private readonly List<BaseUnit> unitsNearSpot = new();

    private int nextSpawnIndex;
    private int sightCursor;

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

    [Export]
    public float EliteGoldFactor { get; set; } = 3f;

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

    [Export]
    public float RareEliteGoldFactor { get; set; } = 8f;

    [ExportGroup("Boss")]
    [Export]
    public float BossScale { get; set; } = 2f;

    [Export]
    public float BossXpFactor { get; set; } = 10f;

    [Export]
    public int BossLootRolls { get; set; } = 6;

    [Export]
    public Color BossNameColor { get; set; } = new(0.85f, 0.2f, 0.4f);

    [Export]
    public float BossGoldFactor { get; set; } = 20f;

    [ExportGroup("Gold")]
    //Elite und Rare Elite lassen immer Gold fallen
    [Export(PropertyHint.Range, "0,100,0.1")]
    public float GoldChancePercent { get; set; } = 50f;

    //Jedes Monsterlevel über 1 hebt den Betrag um diesen Anteil
    [Export(PropertyHint.Range, "0,1,0.01,or_greater")]
    public float GoldGrowthPerLevel { get; set; } = 0.15f;

    [ExportGroup("Sicht")]
    //Gegner zeigen sich bis zu diesem Vielfachen des Lichtradius. 0 hebt die Grenze auf
    [Export(PropertyHint.Range, "0,3,0.05,or_greater")]
    public float SightRadiusFactor { get; set; } = 1.2f;

    //Auf den letzten Metern davor blenden sie ein, mit demselben Punktmuster wie die Mauern
    [Export(PropertyHint.Range, "0,5,0.1,or_greater")]
    public float SightFadeMeters { get; set; } = WallFadeRule.EdgeMeters;

    public SightRange Sight => SightRange.From(Hero.LightRadiusMeters, SightRadiusFactor, SightFadeMeters);

    public IReadOnlyList<Enemy> Enemies => enemies;

    public event Action<Enemy> EnemyKilled;

    public override void _Ready()
    {
        EnemyContainer.ChildEnteredTree += NoteEffect;
        EnemyContainer.ChildExitingTree += child => effects.Remove(child as Node3D);

        if (SpawnMarkers is not null)
            SpawnFrom(SpawnMarkers.GetAllChildren<SpawnMarker>());
    }

    //Neben den Gegnern hängen hier ihre Wirkungen: Projektile und Flächen
    private void NoteEffect(Node child)
    {
        if (child is not Node3D effect || child is Enemy)
            return;

        effects.Add(effect);

        Callable.From(() => Show(effect)).CallDeferred();
    }

    private void Show(Node3D effect)
    {
        if (IsInstanceValid(effect) && effect.IsInsideTree())
            effect.Visible = CanHeroSee(effect.GlobalPosition);
    }

    public void SpawnFrom(IEnumerable<SpawnMarker> markers)
    {
        foreach (var marker in markers)
            SpawnGroupAt(marker);
    }

    //Räumt die Karte für die nächste Ebene. Niemand stirbt dabei, es gibt weder XP noch Beute
    public void Clear()
    {
        foreach (var enemy in enemies)
        {
            enemy.Provoked -= CallGroupToArms;
            enemy.Died     -= OnEnemyDied;
        }

        enemies.Clear();

        nextSpawnIndex = 0;

        foreach (var child in EnemyContainer.GetChildren())
        {
            EnemyContainer.RemoveChild(child);

            child.QueueFree();
        }
    }

    //Für Gegner, die bei einem früheren Besuch der Ebene gefallen sind
    public void Remove(Enemy enemy)
    {
        if (!enemies.Remove(enemy))
            return;

        enemy.Provoked -= CallGroupToArms;
        enemy.Died     -= OnEnemyDied;

        EnemyContainer.RemoveChild(enemy);

        enemy.QueueFree();
    }

    public override void _Process(double delta)
    {
        var sight = Sight;

        UnitSight.Update(Hero.GlobalPosition, sight);

        foreach (var enemy in enemies)
        {
            if (enemy.IsSeen)
                enemy.SetVisibility(sight.GetVisibility(GetDistanceToHero(enemy.GlobalPosition)));
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        var heroPosition = Hero.GlobalPosition;
        var sight        = Sight;

        //Über den Index, weil ein Monster beim Denken weitere beschwören kann
        for (var i = 0; i < enemies.Count; i++)
        {
            var enemy   = enemies[i];
            var isAwake = !enemy.IsResting || WorldScale.GroundDistancePx(enemy.GlobalPosition, heroPosition) <= SimulationRadius;

            enemy.SetAwake(isAwake);

            if (isAwake)
                enemy.Think(delta);

            if (!IsInRange(enemy, sight))
                enemy.SetSeen(false);
        }

        LookAround(sight);
    }

    //Wer in Reichweite steht, den könnte noch eine Mauer verdecken. Das prüft der Held reihum, damit nicht in jedem Schritt alle auf einmal an der Reihe sind
    private void LookAround(SightRange sight)
    {
        var checks = 0;

        for (var step = 0; step < enemies.Count && checks < SightChecksPerFrame; step++)
        {
            sightCursor = (sightCursor + 1) % enemies.Count;

            var enemy = enemies[sightCursor];

            if (!IsInRange(enemy, sight))
                continue;

            enemy.SetSeen(IsUncovered(enemy));

            checks++;
        }

        foreach (var effect in effects)
            Show(effect);
    }

    //Der Held sieht, wer nah genug steht und wen keine Mauer verdeckt
    public bool CanHeroSee(BaseUnit unit)
        => IsInRange(unit, Sight) && IsUncovered(unit);

    public bool CanHeroSee(Vector3 point)
        => Sight.Reaches(GetDistanceToHero(point)) && IsUncovered(point);

    private bool IsInRange(BaseUnit unit, SightRange sight)
        => sight.Reaches(GetDistanceToHero(unit.GlobalPosition), unit.BodyRadius);

    private float GetDistanceToHero(Vector3 point)
        => WorldScale.OnGround(point - Hero.GlobalPosition).Length();

    //Frei steht, wer mit dem Helden im selben Raum steht oder wen keine Mauer verdeckt. Es reicht, wenn ein Rand des Körpers hervorschaut
    private bool IsUncovered(BaseUnit unit)
    {
        if (IsInRoomOfHero(unit.GlobalPosition))
            return true;

        var space  = EnemyContainer.GetWorld3D().DirectSpaceState;
        var eye    = Hero.GlobalPosition + Vector3.Up * EyeHeightMeters;
        var center = unit.BodyCenter;
        var aside  = WorldScale.OnGround(center - eye).Normalized().Cross(Vector3.Up) * unit.BodyRadius;

        return IsInSight(space, eye, center) || IsInSight(space, eye, center + aside) || IsInSight(space, eye, center - aside);
    }

    private bool IsUncovered(Vector3 point)
        => IsInRoomOfHero(point) || IsInSight(EnemyContainer.GetWorld3D().DirectSpaceState, Hero.GlobalPosition + Vector3.Up * EyeHeightMeters, point + Vector3.Up * SightHeightMeters);

    private bool IsInRoomOfHero(Vector3 point)
    {
        var room = RoomZone.GetIdAt(Hero.GlobalPosition);

        return room != WallOpeningRule.NoRoom && RoomZone.GetIdAt(point) == room;
    }

    private static bool IsInSight(PhysicsDirectSpaceState3D space, Vector3 eye, Vector3 point)
        => space.IntersectRay(PhysicsRayQueryParameters3D.Create(eye, point, CollisionLayers.Walls)).Count == 0;

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
            //Auch ein Boss verbraucht die beiden Würfe, damit die übrigen Spawns der Ebene bleiben, wo sie sind
            var modCount = EnemyRarityRules.RollModCount(chances, GameRandom.Shared);
            var area     = new SpawnArea(marker.GlobalPosition, marker.ScatterRadius, marker.MinGap, marker.Enemy.AggroRange + AggroMarginPx);
            var enemy    = marker.Enemy.IsBoss
                                   ? Spawn(marker.Enemy, area, marker.Name, level, marker.Enemy.FixedMods.Where(mod => mod is not null).ToList(), EnemyRarity.Boss)
                                   : Spawn(marker.Enemy, area, marker.Name, level, modCount);

            enemy.SpawnIndex = nextSpawnIndex++;
        }
    }

    public Enemy Spawn(EnemyResource definition, Vector3 position, string spawnGroup, int level, int modCount = 0)
        => Spawn(definition, new SpawnArea(position), spawnGroup, level, modCount);

    public Enemy Spawn(EnemyResource definition, SpawnArea area, string spawnGroup, int level, int modCount = 0)
    {
        var traits = new MonsterTraits(level, definition.Core.UsesProjectiles);
        var mods   = MonsterModRoller.Pick(MonsterModLibrary.Pool, modCount, traits, GameRandom.Shared)
                                     .Select(mod => MonsterModLibrary.Find(mod.Id))
                                     .ToList();

        return Spawn(definition, area, spawnGroup, level, mods);
    }

    public Enemy Spawn(EnemyResource definition, Vector3 position, string spawnGroup, int level, IReadOnlyList<MonsterModResource> mods)
        => Spawn(definition, new SpawnArea(position), spawnGroup, level, mods);

    public Enemy Spawn(EnemyResource definition, SpawnArea area, string spawnGroup, int level, IReadOnlyList<MonsterModResource> mods)
        => Spawn(definition, area, spawnGroup, level, mods, EnemyRarityRules.FromModCount(mods.Count));

    public Enemy Spawn(EnemyResource definition, SpawnArea area, string spawnGroup, int level, IReadOnlyList<MonsterModResource> mods, EnemyRarity rarity)
    {
        var enemy = definition.Scene.Instantiate<Enemy>();
        var look  = GetLookOf(rarity);

        enemy.Configure(definition, level, mods, look, rarity);

        enemy.Position   = FindFreeSpot(area, enemy.GetBodyRadius() * look.Scale);
        enemy.SpawnGroup = spawnGroup;
        enemy.Controller = this;
        enemy.Target     = Hero;
        enemy.Provoked   += CallGroupToArms;
        enemy.Died       += OnEnemyDied;

        enemies.Add(enemy);
        EnemyContainer.AddChild(enemy);

        enemy.SetSeen(CanHeroSee(enemy));

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
            EnemyRarity.Elite     => new EnemyRarityLook(EliteScale, EliteXpFactor, EliteLootRolls, EliteNameColor, EliteGoldFactor),
            EnemyRarity.RareElite => new EnemyRarityLook(RareEliteScale, RareEliteXpFactor, RareEliteLootRolls, RareEliteNameColor, RareEliteGoldFactor),
            EnemyRarity.Boss      => new EnemyRarityLook(BossScale, BossXpFactor, BossLootRolls, BossNameColor, BossGoldFactor),
            _                     => EnemyRarityLook.Normal
        };

    private void OnEnemyDied(BaseUnit unit)
    {
        if (unit is not Enemy enemy)
            return;

        enemies.Remove(enemy);

        CallGroupToArms(enemy);

        Hero.GainExperience(enemy.XpGranted);

        EnemyKilled?.Invoke(enemy);

        //Died feuert genau einmal pro Gegner, daher entsteht der Loot hier und nicht bei jeder Lebensänderung
        SpawnLootbags(enemy);
        DropGold(enemy);
    }

    //Beschworene Gegner tragen kein Gold, sonst wäre ein Beschwörer eine Quelle ohne Ende
    private void DropGold(Enemy enemy)
    {
        if (enemy.SpawnIndex < 0 || enemy.Definition is null)
            return;

        var chance = enemy.Rarity == EnemyRarity.Normal ? GoldChancePercent : 100f;
        var amount = GoldDropRule.Roll(enemy.Definition.GoldMin, enemy.Definition.GoldMax, enemy.Level, GoldGrowthPerLevel, enemy.GoldFactor, chance, GameRandom.Shared);

        if (amount > 0)
            CoinPile.DropAround(Hero.GetParent<Node3D>(), enemy.GlobalPosition, amount, Hero);
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
