using System.Collections.Generic;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Resources.Enemies;
using Hoellenspiralenspiel.Resources.MonsterMods;
using Hoellenspiralenspiel.Scripts.Core.Enemies;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Enemies;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Objects;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.Units.Enemies;
using Hoellenspiralenspiel.Scripts.World;

namespace Hoellenspiralenspiel.Scripts.Controllers;

public partial class EnemyController : Node
{
    private const float LootSpreadMeters = 0.48f;

    private readonly List<Enemy> enemies = new();

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

            Spawn(marker.Enemy, marker.GetSpawnPosition(i), marker.Name, level, modCount);
        }
    }

    public Enemy Spawn(EnemyResource definition, Vector3 position, string spawnGroup, int level, int modCount = 0)
    {
        var traits = new MonsterTraits(level, definition.UsesProjectiles);
        var mods   = MonsterModRoller.Pick(MonsterModLibrary.Pool, modCount, traits, GameRandom.Shared)
                                     .Select(mod => MonsterModLibrary.Find(mod.Id))
                                     .ToList();

        return Spawn(definition, position, spawnGroup, level, mods);
    }

    public Enemy Spawn(EnemyResource definition, Vector3 position, string spawnGroup, int level, IReadOnlyList<MonsterModResource> mods)
    {
        var enemy = definition.Scene.Instantiate<Enemy>();

        enemy.Configure(definition, level, mods, GetLookOf(EnemyRarityRules.FromModCount(mods.Count)));

        enemy.Position   = position;
        enemy.SpawnGroup = spawnGroup;
        enemy.Controller = this;
        enemy.Target     = Hero;
        enemy.Provoked   += CallGroupToArms;
        enemy.Died       += OnEnemyDied;

        enemies.Add(enemy);
        EnemyContainer.AddChild(enemy);

        return enemy;
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

        for (var i = 0; i < loot.Count; i++)
            Lootbag.Drop(Hero.GetParent(), enemy.GlobalPosition + GetLootbagOffset(i, loot.Count), loot[i], Hero.Items);
    }

    //Mehrere Beutel werden im Kreis um den Gegner verteilt, damit sie sich nicht überdecken
    private static Vector3 GetLootbagOffset(int index, int totalAmount)
    {
        if (totalAmount <= 1)
            return Vector3.Zero;

        return Vector3.Right.Rotated(Vector3.Up, Mathf.Tau * index / totalAmount) * LootSpreadMeters;
    }
}
