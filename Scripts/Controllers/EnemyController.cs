using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.Units.Enemies;

namespace Hoellenspiralenspiel.Scripts.Controllers;

public partial class EnemyController : Node
{
    private readonly int                   calculationMaxTries = 20;
    private readonly Random                isEliteRng          = new();
    private readonly Random                isRareRng           = new();
    private readonly RandomNumberGenerator rng                 = new();
    private          Node                  container;
    private          Node                  currentScene;
    private          Player2D              player;
    private          Timer                 spawnTimer;

    [Export]
    public PackedScene EnemyToSpawn { get; set; }

    [Export]
    public float SpawnIntervallSec { get; set; } = 1.5f;

    [Export]
    public float MinDistanceToPlayer { get; set; } = 220f;

    public  List<BaseEnemy> SpawnedEnemies { get; set; } = new();
    private bool            NextSpawnIsRare         => isRareRng.Next(1, 11) == 1;
    private bool            NextSpawnIsElite        => isEliteRng.Next(1, 16) == 1;

    public override void _Ready()
    {
        base._Ready();

        rng.Randomize();

        currentScene = GetTree().CurrentScene;
        player       = currentScene.GetNode<Player2D>("Player 2D");
        container    = GetNode<Node>("Container");
        spawnTimer   = GetNode<Timer>("EnemySpawnTimer");

        spawnTimer.WaitTime =  SpawnIntervallSec;
        spawnTimer.Timeout  += SpawnTimerOnTimeout;
    }

    public override void _PhysicsProcess(double delta) => MakeEnemiesDoTheirThing();

    private void SpawnTimerOnTimeout() => SpawnUnit<TestEnemy>();

    private void SpawnUnit<T>(int amountToSpawn = 1)
            where T : BaseEnemy
    {
        for (var i = 0; i < amountToSpawn; i++)
        {
            var spawn = EnemyToSpawn.Instantiate<T>();

            if(NextSpawnIsRare)
                spawn.MakeRare();
            if(NextSpawnIsElite)
                spawn.MakeElite();

            spawn.Position = GetRandomVisiblePointNotNearPlayer();

            container.AddChild(spawn);
            SpawnedEnemies.Add(spawn);
        }
    }

    private void MakeEnemiesDoTheirThing()
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