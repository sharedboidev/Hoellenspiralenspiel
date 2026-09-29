using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Resources.Levels;
using Hoellenspiralenspiel.Scripts.Controllers;
using Hoellenspiralenspiel.Scripts.Core.Levels;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Environment;
using Hoellenspiralenspiel.Scripts.Objects;
using Hoellenspiralenspiel.Scripts.Skills.Effects;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.World.Levels;

//Führt den Helden von der Oberfläche Ebene um Ebene hinab. Held, Oberfläche und Steuerung bleiben, nur die Ebene wechselt
public partial class Descent : Node
{
    private const int PoolSeedOffset = 104729;

    private Cell?             lastCell;
    private float             lastRevealRadius;
    private AudioStreamPlayer music;
    private RoomLibrary       rooms;
    private int               surfaceAreaLevel;

    [Export]
    public Hero Hero { get; set; }

    [Export]
    public EnemyController Enemies { get; set; }

    [Export]
    public LevelNavigation Navigation { get; set; }

    //Hier hinein entsteht die Ebene. Was vorher darunter hing, weicht ihr
    [Export]
    public Node3D LevelRoot { get; set; }

    [Export]
    public CellarDoor Entrance { get; set; }

    [Export]
    public LevelThemeResource Theme { get; set; }

    [Export]
    public WorldEnvironment Surroundings { get; set; }

    [Export]
    public DirectionalLight3D Moonlight { get; set; }

    [ExportGroup("Generator")]
    //0 würfelt bei jedem Abstieg neu
    [Export]
    public int Seed { get; set; }

    [Export]
    public int RoomCount { get; set; } = 10;

    [Export]
    public int RoomsMorePerDepth { get; set; } = 1;

    [Export]
    public int MaxRoomCount { get; set; } = 24;

    [Export]
    public int MinGap { get; set; } = 2;

    [Export]
    public int MaxGap { get; set; } = 4;

    [Export(PropertyHint.Range, "0,1,0.05")]
    public float LoopShare { get; set; } = 0.35f;

    [Export]
    public int CellsPerCorridorPack { get; set; } = 14;

    public DescentState State { get; } = new();

    public BuiltLevel Level { get; private set; }

    public ExplorationMap Exploration { get; private set; }

    public event Action LevelEntered;
    public event Action Explored;

    public override void _Ready()
    {
        surfaceAreaLevel = Enemies?.AreaLevel ?? 1;

        if (Entrance is not null)
            Entrance.Opened += _ => Descend();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Level is null || !IsInstanceValid(Hero))
            return;

        var cell   = Level.Grid.GetCell(Hero.GlobalPosition);
        var radius = Hero.LightRadiusMeters / LevelGrid.CellMeters;

        if (cell == lastCell && Mathf.IsEqualApprox(radius, lastRevealRadius))
            return;

        lastCell         = cell;
        lastRevealRadius = radius;

        if (Exploration.RevealAround(Level.Layout, cell, radius) > 0)
            Explored?.Invoke();
    }

    public void Descend()
    {
        if (!State.IsBelowGround)
            State.Begin(Seed != 0 ? Seed : (int)GD.Randi());

        Enter(State.Depth + 1);
    }

    //Wer eine Tür benutzt, steckt mitten in der Physik. Abgebaut und gebaut wird deshalb erst danach
    public void Enter(int depth)
        => Callable.From(() => Rebuild(depth)).CallDeferred();

    //Eine Ebene aus einem früheren Abstieg gehört nicht mehr dazu, ihre Karte verfällt
    public void RememberExploration()
    {
        if (Level is not null && Level.Layout.Seed == State.GetSeedOf(Level.Depth))
            State.Remember(Level.Depth, Exploration);
    }

    private void Rebuild(int depth)
    {
        if (Theme is null || LevelRoot is null)
        {
            GD.PushError("Dem Abstieg fehlen das Thema oder der Knoten für die Ebene.");

            return;
        }

        rooms ??= new RoomLibrary(Theme.Rooms);

        var seed     = State.GetSeedOf(depth);
        var settings = GetSettings(depth);
        var layout   = TryGenerate(settings, seed);

        if (layout is null)
            return;

        RememberExploration();
        ClearLevel();

        State.GoTo(depth);
        GameRandom.Reseed(seed);

        Level       = LevelBuilder.Build(layout, rooms, Theme, LevelRoot, depth);
        Exploration = new ExplorationMap(layout.Width, layout.Height);
        lastCell    = null;

        Exploration.TryRestore(State.GetRevealed(depth));

        foreach (var exit in Level.Exits)
            exit.Opened += _ => Descend();

        ApplyLook();

        Enemies.AreaLevel = settings.AreaLevel;

        Hero.MoveToLevelStart(Level.HeroStart);
        Navigation?.Rebuild();

        LevelEntered?.Invoke();

        Populate(Level);
    }

    //Scheitert der Generator, bleibt der Held, wo er ist
    private LevelLayout TryGenerate(LevelSettings settings, int seed)
    {
        try
        {
            return LevelGenerator.Generate(rooms.Blueprints, settings, seed);
        }
        catch (Exception exception) when (exception is LevelGenerationException or ArgumentOutOfRangeException)
        {
            GD.PushError($"Die Ebene mit dem Seed {seed} lässt sich nicht erzeugen: {exception.Message}");

            return null;
        }
    }

    private LevelSettings GetSettings(int depth)
        => new()
        {
            RoomCount            = Math.Min(MaxRoomCount, RoomCount + RoomsMorePerDepth * (depth - 1)),
            MinGap               = MinGap,
            MaxGap               = MaxGap,
            LoopShare            = LoopShare,
            CellsPerCorridorPack = CellsPerCorridorPack,
            AreaLevel            = surfaceAreaLevel + depth
        };

    //Erst nach einem Schritt der Physik stehen die Mauern so, dass die Suche nach freien Plätzen sie sieht
    private async void Populate(BuiltLevel level)
    {
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

        if (level != Level || !IsInstanceValid(level.Root))
            return;

        var random = new SeededRandom(unchecked(level.Layout.Seed + PoolSeedOffset));

        foreach (var marker in level.RoomMarkers.Where(marker => marker.Enemy is null))
            marker.Enemy = PickFromPool(random)?.Enemy;

        foreach (var marker in level.CorridorMarkers)
        {
            var entry = PickFromPool(random);

            marker.Enemy         = entry?.Enemy;
            marker.AmountToSpawn = entry is null ? 0 : random.NextInt(entry.MinGroupSize, entry.MaxGroupSize + 1);
        }

        Enemies.SpawnFrom(level.RoomMarkers.Concat(level.CorridorMarkers).Where(marker => marker.Enemy is not null));
    }

    private EnemyPoolEntry PickFromPool(IRandomSource random)
    {
        var allowed = Theme.Enemies.Where(entry => entry?.Enemy is not null && entry.MinAreaLevel <= Enemies.AreaLevel).ToList();
        var total   = allowed.Sum(entry => Math.Max(0f, entry.Weight));
        var roll    = random.NextFloat() * total;

        foreach (var entry in allowed)
        {
            roll -= Math.Max(0f, entry.Weight);

            if (roll < 0f)
                return entry;
        }

        return allowed.LastOrDefault();
    }

    private void ClearLevel()
    {
        Enemies.Clear();

        foreach (var lootbag in Lootbag.Lying.ToList())
            Remove(lootbag);

        foreach (var effect in Hero.GetParent().GetChildren().Where(child => child is SkillArea or SkillProjectile).ToList())
            Remove(effect);

        foreach (var child in LevelRoot.GetChildren())
            Remove(child);

        if (IsInstanceValid(Entrance))
            Remove(Entrance);

        Entrance = null;
        Level    = null;
    }

    //Erst aushängen, dann freigeben: Die Navigation und die Suche nach Plätzen sähen sonst noch die alten Mauern
    private static void Remove(Node node)
    {
        node.GetParent()?.RemoveChild(node);

        node.QueueFree();
    }

    private void ApplyLook()
    {
        if (Surroundings?.Environment is { } environment)
        {
            environment.AmbientLightColor  = Theme.AmbientColor;
            environment.AmbientLightEnergy = Theme.AmbientEnergy;
            environment.FogLightColor      = Theme.FogColor;
        }

        if (Moonlight is not null)
        {
            Moonlight.LightColor  = Theme.MoonlightColor;
            Moonlight.LightEnergy = Theme.MoonlightEnergy;
        }

        PlayMusic();
    }

    private void PlayMusic()
    {
        if (Theme.Music is null || music?.Stream == Theme.Music)
            return;

        if (music is null)
        {
            music = new AudioStreamPlayer { Name = "Music" };

            AddChild(music);
        }

        music.Stream = Theme.Music;

        music.Play();
    }
}
