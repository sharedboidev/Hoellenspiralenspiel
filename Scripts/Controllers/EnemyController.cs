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
    public PackedScene EnemyToSpawn { get; set; }

    [Export]
    public Lootsystem Lootsystem { get; set; }

    [Export]
    public Inventory Inventory { get; set; }

    private PackedScene LootbagScene { get; set; } = ResourceLoader.Load<PackedScene>("res://Scenes/Objects/lootbag.tscn");

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

        currentScene = GetTree().CurrentScene;
        player       = currentScene.GetNode<Player2D>("%Player 2D");
        container    = currentScene.GetNode<Node2D>("%Enemies");
        spawnTimer   = GetNode<Timer>("EnemySpawnTimer");

        spawnTimer.WaitTime =  SpawnIntervallSec;
        spawnTimer.Timeout  += SpawnTimerOnTimeout;
    }

    public override void _PhysicsProcess(double delta) => MakeEnemiesDoTheirThing(delta);

    private void SpawnTimerOnTimeout()
    {
        if (SpawnedEnemies.Count >= 100)
            return;

        SpawnUnit<TestEnemy>();
    }

    private void SpawnUnit<T>(int amountToSpawn = 1)
            where T : BaseEnemy
    {
        for (var i = 0; i < amountToSpawn; i++)
        {
            var spawn = EnemyToSpawn.Instantiate<T>();

            if (NextSpawnIsRare)
                spawn.MakeRare();

            if (NextSpawnIsElite)
                spawn.MakeElite();

            spawn.Position        =  GetRandomVisiblePointNotNearPlayer();
            spawn.PropertyChanged += SpawnOnPropertyChanged;

            SpawnedEnemies.Add(spawn);
            container.AddChild(spawn);
        }
    }

    private void SpawnOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(BaseEnemy.LifeCurrent) || sender is not BaseEnemy { LifeCurrent: <= 0 } enemy)
            return;

        SpawnLootbag(enemy);
    }

    private void SpawnLootbag(BaseEnemy enemy)
    {
        var loot = Lootsystem.GenerateLoot(enemy);

        var lootbagInstance = LootbagScene.Instantiate<Lootbag>();
        lootbagInstance.GlobalPosition =  enemy.GlobalPosition;
        lootbagInstance.ContainedItem  =  loot.FirstOrDefault();
        lootbagInstance.LootClicked    += LootbagInstanceOnLootClicked;

        GetParent().GetNode<Node2D>("Environment").AddChild(lootbagInstance);
    }

    private void LootbagInstanceOnLootClicked(Lootbag sender, BaseItem lootedItem)
    {
        GD.Print($"{lootedItem?.Name ?? "Nothing"} looted by {player?.Name}");

        Inventory.SetItem(lootedItem);

        sender?.QueueFree();
    }

    private void MakeEnemiesDoTheirThing(double delta)
    {
        foreach (var aggressiveEnemy in SpawnedEnemies.Where(e => e.IsAggressive))
            aggressiveEnemy.ChasePlayer();
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