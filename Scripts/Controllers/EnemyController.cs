using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Scripts.Extensions;
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
    public PackedScene[] EnemiesToSpawn { get; set; }

    [Export]
    public Lootsystem Lootsystem { get; set; }

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
            spawn.Died += SpawnOnDied;

            SpawnedEnemies.Add(spawn);
            container.AddChild(spawn);
        }
    }

    private void SpawnOnDied(BaseUnit unit)
    {
        if(unit is not BaseEnemy enemy)
            return;

        AggroMyGroup(enemy);
        player.GainExperience(enemy.XpGranted);

        //Died feuert genau einmal pro Gegner, daher entsteht der Loot hier und nicht bei jeder Lebensänderung
        SpawnLootbags(enemy);
    }

    private void SpawnOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(BaseEnemy.LifeCurrent) || sender is not BaseEnemy enemy)
            return;

        if (enemy.IsAggressive)
            AggroMyGroup(enemy);
    }

    private void AggroMyGroup(BaseEnemy hitEnemy)
    {
        var notAggressiveFriends = SpawnedEnemies.Except([hitEnemy])
                                                 .Where(friends => !friends.IsAggressive &&
                                                                   friends.SpawnGroup == hitEnemy.SpawnGroup);

        foreach (var groupMember in notAggressiveFriends)
            groupMember.IsAggressive = true;
    }

    private void SpawnLootbags(BaseEnemy enemy)
    {
        var loot = Lootsystem.GenerateLoot(enemy);

        for (var i = 0; i < loot.Count; i++)
            Lootbag.Drop(player.GetParent(), enemy.GlobalPosition + GetLootbagOffset(i, loot.Count), loot[i], player.Items);
    }

    //Mehrere Beutel werden im Kreis um den Gegner verteilt, damit sie sich nicht überdecken
    private static Vector2 GetLootbagOffset(int index, int totalAmount)
    {
        const float spreadRadiusPx = 48f;

        if (totalAmount <= 1)
            return Vector2.Zero;

        return Vector2.Right.Rotated(Mathf.Tau * index / totalAmount) * spreadRadiusPx;
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