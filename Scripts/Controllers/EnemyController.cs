using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Items;
using Hoellenspiralenspiel.Scripts.Objects;
using Hoellenspiralenspiel.Scripts.UI;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.Units.Enemies;
using CharacterSheet = Hoellenspiralenspiel.Scripts.UI.Character.CharacterSheet;
using Inventory = Hoellenspiralenspiel.Scripts.UI.Character.Inventory;

namespace Hoellenspiralenspiel.Scripts.Controllers;

public partial class EnemyController : Node
{
    private readonly int                   calculationMaxTries = 20;
    private readonly Random                isEliteRng          = new();
    private readonly Random                isRareRng           = new();
    private readonly RandomNumberGenerator rng                 = new();
    private          Node2D                container;
    private          Node                  currentScene;
    private          Player2D              player;
    private          Timer                 spawnTimer;

    [Export]
    public PackedScene[] EnemiesToSpawn { get; set; }

    [Export]
    public Lootsystem Lootsystem { get; set; }

    [Export]
    public CharacterSheet CharacterSheet { get; set; }

    private PackedScene LootbagScene { get; set; }

    [Export]
    public float SpawnIntervallSec { get; set; } = 1.5f;

    [Export]
    public float MinDistanceToPlayer { get; set; } = 220f;

    public  List<BaseEnemy> SpawnedEnemies   { get; set; } = new();
    private bool            NextSpawnIsRare  => isRareRng.Next(1, 11) == 1;
    private bool            NextSpawnIsElite => isEliteRng.Next(1, 16) == 1;

    public override void _Ready()
    {
        base._Ready();

        rng.Randomize();

        LootbagScene = ResourceLoader.Load<PackedScene>("res://Scenes/Objects/lootbag.tscn");
        currentScene = GetTree().CurrentScene;
        player       = currentScene.GetNode<Player2D>("%Player 2D");
        container    = currentScene.GetNode<Node2D>("%Enemies");

        //ConfigureSpawntimer();

        var spawnMarkers = GetParent().GetNode<Node2D>(nameof(SpawnMarker)).GetAllChildren<SpawnMarker>();

        foreach (var spawnMarker in spawnMarkers)
            SpawnEnemies(spawnMarker);
    }

    private void ConfigureSpawntimer()
    {
        spawnTimer = GetNode<Timer>("EnemySpawnTimer");

        spawnTimer.WaitTime =  SpawnIntervallSec;
        spawnTimer.Timeout  += SpawnTimerOnTimeout;
    }

    public override void _PhysicsProcess(double delta)
        => MakeEnemiesDoTheirThing(delta);

    private void SpawnTimerOnTimeout()
    {
        if (SpawnedEnemies.Count >= 100)
            return;
    }

    private void SpawnEnemies(SpawnMarker spawnMarker)
    {
        for (var i = 0; i < spawnMarker.AmountToSpawn; i++)
        {
            var spawn = spawnMarker.EnemyToSpawn.Instantiate<BaseEnemy>();

            if (NextSpawnIsRare)
                spawn.MakeRare();

            if (NextSpawnIsElite)
                spawn.MakeElite();

            spawn.Position        =  spawnMarker.GetSpawnlocationFor(i);
            spawn.SpawnGroup      =  spawnMarker.Name;
            spawn.PropertyChanged += SpawnOnPropertyChanged;

            SpawnedEnemies.Add(spawn);
            container.AddChild(spawn);
        }
    }

    private void SpawnOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(BaseEnemy.LifeCurrent) || sender is not BaseEnemy enemy)
            return;

        if (enemy.LifeCurrent <= 0)
            SpawnLootbag(enemy);
        else
        {
            if (enemy.LifeCurrent < enemy.LifeMaximum)
                AggroMyGroup(enemy);
        }
    }

    private void AggroMyGroup(BaseEnemy hitEnemy)
    {
        var notAggressiveFriends = SpawnedEnemies.Except([hitEnemy])
                                                 .Where(friends => !friends.IsAggressive &&
                                                                   friends.SpawnGroup == hitEnemy.SpawnGroup);

        foreach (var groupMember in notAggressiveFriends)
            groupMember.IsAggressive = true;
    }

    private void SpawnLootbag(BaseEnemy enemy)
    {
        var loot = Lootsystem.GenerateLoot(enemy);

        if (loot is null || loot.Length == 0)
            return;

        InstantiateLootbag(enemy, loot[0]);
    }

    private void InstantiateLootbag(BaseEnemy enemy, BaseItem loot)
    {
        var lootbagInstance = LootbagScene.Instantiate<Lootbag>();
        lootbagInstance.GlobalPosition =  enemy.GlobalPosition;
        lootbagInstance.ContainedItem  =  loot;
        lootbagInstance.LootClicked    += LootbagInstanceOnLootClicked;

        GetParent().GetNode<Node2D>("Environment").AddChild(lootbagInstance);
    }

    private void LootbagInstanceOnLootClicked(Lootbag sender, BaseItem lootedItem)
    {
        GD.Print($"{lootedItem?.Name ?? "Nothing"} looted.");

        var inventory = CharacterSheet.GetNode<Inventory>("%" + nameof(Inventory));
        inventory.SetItem(lootedItem);

        sender?.QueueFree();
    }

    private void MakeEnemiesDoTheirThing(double delta)
    {
        foreach (var enemy in SpawnedEnemies)
        {
            if (player.IsInAggroRangeOf(enemy))
                enemy.IsAggressive = true;

            enemy.ChasePlayer();
        }
    }

    private Vector2 GetRandomVisiblePointNotNearPlayer()
    {
        var rect    = GetViewport().GetVisibleRect();
        var padding = 64f;
        rect.Position += new Vector2(padding, padding);
        rect.Size     -= new Vector2(padding * 2f, padding * 2f);

        if (rect.Size.X <= 0 || rect.Size.Y <= 0)
            return Vector2.Zero;

        var minDistSq = MinDistanceToPlayer * MinDistanceToPlayer;
        var playerPos = player.GlobalPosition;

        for (var i = 0; i < calculationMaxTries; i++)
        {
            var x = rng.RandfRange(rect.Position.X, rect.End.X);
            var y = rng.RandfRange(rect.Position.Y, rect.End.Y);
            var p = new Vector2(x, y);

            if (p.DistanceSquaredTo(playerPos) >= minDistSq)
                return p;
        }

        return Vector2.Zero;
    }
}