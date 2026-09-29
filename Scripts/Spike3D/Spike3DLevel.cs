using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Resources.Enemies;
using Hoellenspiralenspiel.Scripts.Core.Enemies;
using Hoellenspiralenspiel.Scripts.Extensions;

namespace Hoellenspiralenspiel.Scripts.Spike3D;

public partial class Spike3DLevel : Node3D
{
    private readonly List<Enemy3D> enemies = new();

    [Export]
    public Hero3D Hero { get; set; }

    [Export]
    public Node3D EnemyContainer { get; set; }

    [Export]
    public int AreaLevel { get; set; } = 1;

    [Export]
    public float SimulationRadiusPx { get; set; } = 2500f;

    [Export]
    public double RespawnDelaySec { get; set; } = 2;

    [Export]
    public ProgressBar LifeBar { get; set; }

    [Export]
    public ProgressBar ManaBar { get; set; }

    [Export]
    public Label Status { get; set; }

    [Export]
    public Ps1Look Look { get; set; }

    public IReadOnlyList<Enemy3D> Enemies => enemies;

    public override void _Ready()
    {
        foreach (var marker in this.GetAllChildren<SpawnMarker3D>())
            SpawnGroupAt(marker);

        Hero.LifeChanged += _ => ShowResources();
        Hero.ManaChanged += ShowResources;
        Hero.Died        += _ => GetTree().CreateTimer(RespawnDelaySec).Timeout += Hero.Respawn;

        ShowResources();
    }

    public override void _Process(double delta)
    {
        if (Status is not null)
            Status.Text = $"{Engine.GetFramesPerSecond():N0} FPS · {enemies.Count} Gegner · {DescribeLook()}";
    }

    public override void _PhysicsProcess(double delta)
    {
        var heroPosition = Hero.GlobalPosition;

        //Über den Index, weil ein Monster beim Denken weitere beschwören kann
        for (var i = 0; i < enemies.Count; i++)
        {
            var enemy   = enemies[i];
            var isAwake = !enemy.IsResting || WorldScale.GroundDistancePx(enemy.GlobalPosition, heroPosition) <= SimulationRadiusPx;

            enemy.SetAwake(isAwake);

            if (isAwake)
                enemy.Think(delta);
        }
    }

    private string DescribeLook()
    {
        if (Look is null || !Look.Enabled)
            return "PS1-Look aus";

        return $"PS1-Look mit {Look.Lines} Zeilen, {(Look.RealShadows ? "Schatten aus Lichtern" : "Scheiben als Schatten")}";
    }

    private void SpawnGroupAt(SpawnMarker3D marker)
    {
        if (marker.Enemy is null || marker.Scene is null)
        {
            GD.PushWarning($"Der Spawn-Marker {marker.Name} hat keinen Gegner oder keine Szene.");

            return;
        }

        var level = EnemyScaling.GetLevel(AreaLevel, marker.Enemy.LevelOffset, marker.LevelOffset);

        for (var i = 0; i < marker.AmountToSpawn; i++)
            Spawn(marker.Enemy, marker.Scene, marker.GetSpawnPosition(i), marker.Name, level);
    }

    public Enemy3D Spawn(EnemyResource definition, PackedScene scene, Vector3 position, string spawnGroup, int level)
    {
        var enemy = scene.Instantiate<Enemy3D>();

        enemy.Configure(definition, level);

        enemy.Position   = position;
        enemy.SpawnGroup = spawnGroup;
        enemy.Target     = Hero;
        enemy.Provoked   += CallGroupToArms;
        enemy.Died       += OnEnemyDied;

        enemies.Add(enemy);
        EnemyContainer.AddChild(enemy);

        return enemy;
    }

    private void OnEnemyDied(Unit3D unit)
    {
        if (unit is not Enemy3D enemy)
            return;

        enemies.Remove(enemy);

        CallGroupToArms(enemy);
    }

    private void CallGroupToArms(Enemy3D caller)
    {
        foreach (var enemy in enemies)
        {
            if (enemy != caller && !enemy.IsInCombat && enemy.SpawnGroup == caller.SpawnGroup)
                enemy.Provoke();
        }
    }

    private void ShowResources()
    {
        if (LifeBar is not null)
        {
            LifeBar.MaxValue = Hero.LifeMaximum;
            LifeBar.Value    = Hero.LifeCurrent;
        }

        if (ManaBar is not null)
        {
            ManaBar.MaxValue = Hero.ManaMaximum;
            ManaBar.Value    = Hero.ManaCurrent;
        }
    }
}
