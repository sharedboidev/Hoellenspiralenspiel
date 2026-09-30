using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using Hoellenspiralenspiel.Resources.Levels;
using Hoellenspiralenspiel.Scripts.Controllers;
using Hoellenspiralenspiel.Scripts.Core.Levels;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Environment;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Objects;
using Hoellenspiralenspiel.Scripts.Skills;
using Hoellenspiralenspiel.Scripts.Skills.Effects;
using Hoellenspiralenspiel.Scripts.UI;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.Units.Enemies;
using Hoellenspiralenspiel.Scripts.Utils;

namespace Hoellenspiralenspiel.Scripts.World.Levels;

public enum ArrivalKind
{
    Start,
    Exit,
    Point,
    CirclePortal,
    TownPortal
}

public readonly record struct Arrival(ArrivalKind Kind, Vector3 Point = default, int CircleNumber = 0)
{
    public static readonly Arrival AtStart      = new(ArrivalKind.Start);
    public static readonly Arrival AtExit       = new(ArrivalKind.Exit);
    public static readonly Arrival AtTownPortal = new(ArrivalKind.TownPortal);

    public static Arrival At(Vector3 point)
        => new(ArrivalKind.Point, point);

    public static Arrival AtPortalOf(int circleNumber)
        => new(ArrivalKind.CirclePortal, CircleNumber: circleNumber);
}

//Führt den Helden zwischen dem Hub und den Ebenen der Kreise hin und her. Held, Oberfläche und Steuerung bleiben, nur der Ort wechselt
public partial class Descent : Node
{
    private const int   PoolSeedOffset       = 104729;
    private const int   SpawnSeedOffset      = 15485863;
    private const float TownPortalGapMeters  = 1.8f;
    private const float SightHeightMeters    = 0.5f;
    private const int   TownPortalDirections = 8;
    private const float SouthDegrees         = 90f;

    private Cell?             lastCell;
    private float             lastRevealRadius;
    private AudioStreamPlayer music;
    private TownPortal        openPortal;
    private SurfaceLook       surfaceLook;

    [Export]
    public Hero Hero { get; set; }

    [Export]
    public EnemyController Enemies { get; set; }

    [Export]
    public LevelNavigation Navigation { get; set; }

    //Hier hinein entsteht der Ort. Was vorher darunter hing, weicht ihm
    [Export]
    public Node3D LevelRoot { get; set; }

    [Export]
    public WorldEnvironment Surroundings { get; set; }

    [Export]
    public DirectionalLight3D Moonlight { get; set; }

    [Export]
    public Curtain Curtain { get; set; }

    [ExportGroup("Orte")]
    [Export]
    public PackedScene Hub { get; set; }

    //Zum Testen mit F6 erreichbar
    [Export]
    public PackedScene TestGrounds { get; set; }

    [Export]
    public Array<LevelThemeResource> Circles { get; set; } = new();

    [ExportGroup("Town-Portal")]
    [Export]
    public PackedScene TownPortalScene { get; set; }

    [Export]
    public double TownPortalCooldownSec { get; set; } = 60;

    [ExportGroup("Generator")]
    //0 würfelt bei jedem neuen Abstieg
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

    public JourneyState Journey { get; } = new();

    public LevelThemeResource Circle { get; private set; }

    public DescentState State => Circle is null ? null : Journey.GetDescent(Circle.Id);

    public BuiltLevel Level { get; private set; }

    public Place Place { get; private set; }

    public ExplorationMap Exploration { get; private set; }

    public bool IsTravelling { get; private set; }

    public double TownPortalCooldownLeftSec { get; private set; }

    public TownPortal OpenPortal => IsInstanceValid(openPortal) ? openPortal : null;

    public bool IsInTestGrounds => Place is not null && TestGrounds is not null && Place.SceneFilePath == TestGrounds.ResourcePath;

    public LevelThemeResource FirstCircle => Circles.Where(circle => circle is not null).OrderBy(circle => circle.Number).FirstOrDefault();

    public event Action LevelEntered;
    public event Action PlaceEntered;
    public event Action Arrived;
    public event Action Explored;

    //Etwas hat sich geändert, das in den Spielstand gehört
    public event Action Changed;

    public event Action<CirclePortal> CirclePortalUsed;

    public override void _Ready()
    {
        surfaceLook = SurfaceLook.From(Surroundings, Moonlight);

        if (Enemies is not null)
            Enemies.EnemyKilled += OnEnemyKilled;
    }

    public override void _Process(double delta)
    {
        if (TownPortalCooldownLeftSec > 0)
            TownPortalCooldownLeftSec = Math.Max(0, TownPortalCooldownLeftSec - delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Level is null || IsTravelling || !IsInstanceValid(Hero))
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

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed(InputActions.OpenTownPortal) && TryOpenTownPortal())
            GetViewport().SetInputAsHandled();
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.F6 } && TestGrounds is not null)
            Show(IsInTestGrounds ? Hub : TestGrounds, Arrival.AtStart);
    }

    public LevelThemeResource FindCircle(string circleId)
        => Circles.FirstOrDefault(circle => circle is not null && circle.Id == circleId);

    public LevelThemeResource FindCircle(int number)
        => Circles.FirstOrDefault(circle => circle is not null && circle.Number == number);

    public void ShowHub()
        => Show(Hub, Arrival.AtStart);

    public void Show(PackedScene scene, Arrival arrival)
    {
        if (scene is null || IsTravelling)
            return;

        var node = scene.Instantiate();

        if (node is not Place place)
        {
            GD.PushError($"Die Szene {scene.ResourcePath} ist kein Ort, an ihrer Wurzel fehlt das Skript {nameof(Place)}.");

            node.Free();

            return;
        }

        Travel(place.DisplayName, string.Empty, () => BuildPlace(place, arrival));
    }

    public void EnterCircle(LevelThemeResource circle, int depth)
        => EnterCircle(circle, depth, Arrival.AtStart);

    public void EnterCircle(LevelThemeResource circle, int depth, Arrival arrival)
    {
        if (circle is null || IsTravelling)
            return;

        Travel(circle.DisplayName, $"Level {ClampDepth(circle, depth)} of {circle.LevelCount}", () => BuildLevel(circle, depth, arrival));
    }

    //Würfelt die Ebenen des Kreises neu. Die Checkpoints bleiben
    public void BeginAnew(LevelThemeResource circle)
    {
        if (circle is null || circle == Circle)
            return;

        Journey.BeginAnew(circle.Id, RollSeed());

        Changed?.Invoke();
    }

    //Eine Ebene aus einem früheren Abstieg gehört nicht mehr dazu, ihre Karte verfällt
    public void RememberExploration()
    {
        if (Level is not null && State is not null && Level.Layout.Seed == State.GetSeedOf(Level.Depth))
            State.Remember(Level.Depth, Exploration);
    }

    public bool TryOpenTownPortal()
    {
        if (Level is null || IsTravelling || TownPortalScene is null || TownPortalCooldownLeftSec > 0 || !IsInstanceValid(Hero) || Hero.IsDead)
            return false;

        var point = FindPlaceForTownPortal();

        Journey.OpenTownPortal(new TownPortalSpot(Circle.Id, Level.Depth, point.X, point.Z));

        StandUpTownPortal(point, false);

        TownPortalCooldownLeftSec = TownPortalCooldownSec;

        Changed?.Invoke();

        return true;
    }

    //Solange der Held reist, steht die Welt still und nimmt keine Eingaben an.
    //Wer eine Tür benutzt, steckt mitten in der Physik. Abgebaut und gebaut wird deshalb erst hinter dem Vorhang
    private async void Travel(string title, string detail, Action build)
    {
        IsTravelling = true;

        Hold(true);

        if (Curtain is null)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        else
        {
            Curtain.Drop(title, detail);

            await Curtain.WaitUntilShown();
        }

        build();
    }

    private void Hold(bool isHeld)
    {
        if (IsInsideTree())
            GetTree().Paused = isHeld;
    }

    //Die Welt bleibt angehalten, nur die Physik tut einen Schritt
    private async Task StepPhysics()
    {
        PhysicsServer3D.SetActive(true);

        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

        PhysicsServer3D.SetActive(!GetTree().Paused);
    }

    private int RollSeed()
        => Seed != 0 ? Seed : (int)GD.Randi();

    private void BuildPlace(Place place, Arrival arrival)
    {
        if (LevelRoot is null)
        {
            GD.PushError("Dem Abstieg fehlt der Knoten für den Ort.");

            place.Free();

            Arrive();

            return;
        }

        LeaveCurrent();

        Place = place;

        LevelRoot.AddChild(place);

        foreach (var portal in place.GetAllChildren<CirclePortal>())
        {
            portal.Circle = FindCircle(portal.Number);

            portal.SetUnlocked(Journey.IsUnlocked(portal.Number));

            portal.Used += used => CirclePortalUsed?.Invoke((CirclePortal)used);
        }

        foreach (var door in place.GetAllChildren<CellarDoor>())
            door.Used += _ => EnterCircle(FirstCircle, 1);

        if (Journey.TownPortal is not null && place.HasTownPortal)
            StandUpTownPortal(place.TownPortalSpot, true);

        surfaceLook.ApplyTo(Surroundings, Moonlight);
        StopMusic();

        Enemies.AreaLevel = place.AreaLevel;

        Hero.MoveToLevelStart(place.HeroStart);
        Navigation?.Rebuild();

        PlaceEntered?.Invoke();

        Populate(place, arrival);
    }

    private void BuildLevel(LevelThemeResource circle, int wantedDepth, Arrival arrival)
    {
        if (LevelRoot is null)
        {
            GD.PushError("Dem Abstieg fehlt der Knoten für die Ebene.");

            Arrive();

            return;
        }

        var state = Journey.GetDescent(circle.Id);

        if (!state.HasBegun)
            Journey.BeginAnew(circle.Id, RollSeed());

        var depth    = ClampDepth(circle, wantedDepth);
        var rooms    = new RoomLibrary(circle.Rooms);
        var seed     = state.GetSeedOf(depth);
        var settings = GetSettings(circle, depth);
        var layout   = TryGenerate(rooms, settings, seed);

        if (layout is null)
        {
            Arrive();

            return;
        }

        LeaveCurrent();

        Circle = circle;

        state.GoTo(depth);
        GameRandom.Reseed(seed);

        Level       = LevelBuilder.Build(layout, rooms, circle, LevelRoot, depth);
        Exploration = new ExplorationMap(layout.Width, layout.Height);
        lastCell    = null;

        Exploration.TryRestore(state.GetRevealed(depth));

        ConnectStairs(Level, circle, depth);

        if (Journey.TownPortal is { } spot && spot.CircleId == circle.Id && spot.Depth == depth)
            StandUpTownPortal(new Vector3(spot.X, 0f, spot.Z), true);

        ApplyLook(circle);

        Enemies.AreaLevel = settings.AreaLevel;

        Hero.MoveToLevelStart(Level.HeroStart);
        Navigation?.Rebuild();

        LevelEntered?.Invoke();

        Populate(Level, arrival);
    }

    private static int ClampDepth(LevelThemeResource circle, int depth)
        => Math.Clamp(depth, 1, Math.Max(1, circle.LevelCount));

    //Die letzte Ebene hat keinen Weg hinab
    private void ConnectStairs(BuiltLevel level, LevelThemeResource circle, int depth)
    {
        foreach (var exit in level.Exits.ToList())
        {
            if (depth < circle.LevelCount)
            {
                exit.Used += _ => EnterCircle(circle, depth + 1);

                continue;
            }

            level.Exits.Remove(exit);

            Remove(exit);
        }

        foreach (var entrance in level.Entrances)
        {
            if (depth > 1)
                entrance.Used += _ => EnterCircle(circle, depth - 1, Arrival.AtExit);
            else
                entrance.Used += _ => Show(Hub, Arrival.AtPortalOf(circle.Number));
        }
    }

    private void LeaveCurrent()
    {
        RememberExploration();

        State?.Leave();

        Enemies.Clear();

        foreach (var lootbag in Lootbag.Lying.ToList())
            Remove(lootbag);

        foreach (var effect in Hero.GetParent().GetChildren().Where(child => child is SkillArea or SkillProjectile).ToList())
            Remove(effect);

        foreach (var sound in GetTree().GetNodesInGroup(SkillArea.LingeringSoundGroup))
            Remove(sound);

        foreach (var child in LevelRoot.GetChildren())
            Remove(child);

        Circle     = null;
        Level      = null;
        Place      = null;
        openPortal = null;
    }

    //Scheitert der Generator, bleibt der Held, wo er ist
    private static LevelLayout TryGenerate(RoomLibrary rooms, LevelSettings settings, int seed)
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

    private LevelSettings GetSettings(LevelThemeResource circle, int depth)
        => new()
        {
            RoomCount            = Math.Min(MaxRoomCount, RoomCount + RoomsMorePerDepth * (depth - 1)),
            MinGap               = MinGap,
            MaxGap               = MaxGap,
            LoopShare            = LoopShare,
            CellsPerCorridorPack = CellsPerCorridorPack,
            AreaLevel            = circle.FirstAreaLevel + depth - 1
        };

    //Erst nach einem Schritt der Physik stehen die Mauern so, dass die Suche nach freien Plätzen sie sieht.
    //Der Held steht beim Spawnen immer am Start, sonst stünden dieselben Gegner bei jeder Ankunft woanders
    private async void Populate(BuiltLevel level, Arrival arrival)
    {
        await StepPhysics();

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

        GameRandom.Reseed(unchecked(level.Layout.Seed + SpawnSeedOffset));

        Enemies.SpawnFrom(level.RoomMarkers.Concat(level.CorridorMarkers).Where(marker => marker.Enemy is not null));

        foreach (var enemy in Enemies.Enemies.Where(enemy => State.IsKilled(level.Depth, enemy.SpawnIndex)).ToList())
            Enemies.Remove(enemy);

        Hero.Teleport(GetArrivalPoint(level, arrival));

        if (arrival.Kind == ArrivalKind.Point)
            CloseTownPortal();

        Arrive();
    }

    private async void Populate(Place place, Arrival arrival)
    {
        await StepPhysics();

        if (place != Place || !IsInstanceValid(place))
            return;

        Enemies.SpawnFrom(place.GetAllChildren<SpawnMarker>().Where(marker => marker.Enemy is not null));

        Hero.Teleport(GetArrivalPoint(place, arrival));

        Arrive();
    }

    //Der Vorhang bleibt, bis man den Tipp lesen konnte. Erst dann läuft die Welt weiter
    private async void Arrive()
    {
        if (Curtain is not null)
            await Curtain.WaitForMinimum();

        Hold(false);

        IsTravelling = false;

        Curtain?.Lift();

        Arrived?.Invoke();
    }

    private static Vector3 GetArrivalPoint(BuiltLevel level, Arrival arrival)
        => arrival.Kind switch
        {
            ArrivalKind.Exit when level.Exits.Count > 0 => level.Exits[0].ArrivalPoint,
            ArrivalKind.Point                           => arrival.Point,
            _                                           => level.HeroStart
        };

    private Vector3 GetArrivalPoint(Place place, Arrival arrival)
    {
        if (arrival.Kind == ArrivalKind.TownPortal && IsInstanceValid(openPortal))
            return openPortal.ArrivalPoint;

        if (arrival.Kind == ArrivalKind.CirclePortal)
        {
            var portal = place.GetAllChildren<CirclePortal>().FirstOrDefault(portal => portal.Number == arrival.CircleNumber);

            if (portal is not null)
                return portal.ArrivalPoint;
        }

        return place.HeroStart;
    }

    private EnemyPoolEntry PickFromPool(IRandomSource random)
    {
        var allowed = Circle.Enemies.Where(entry => entry?.Enemy is not null && entry.MinAreaLevel <= Enemies.AreaLevel).ToList();
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

    private void OnEnemyKilled(Enemy enemy)
    {
        if (Level is not null && State.RememberKill(Level.Depth, enemy.SpawnIndex))
            Changed?.Invoke();
    }

    #region Town-Portal

    //Im Hub führt das Portal zurück an die Stelle, an der es geöffnet wurde, und schließt sich dann
    private void StandUpTownPortal(Vector3 point, bool isOpenAtOnce)
    {
        if (TownPortalScene is null)
            return;

        if (IsInstanceValid(openPortal))
            Remove(openPortal);

        openPortal = TownPortalScene.Instantiate<TownPortal>();

        ((Node3D)Level?.Root ?? Place).AddChild(openPortal);

        openPortal.GlobalPosition = point;

        if (isOpenAtOnce)
            openPortal.OpenAtOnce();

        openPortal.Used += _ => GoThroughTownPortal();
    }

    private void GoThroughTownPortal()
    {
        if (Level is not null)
        {
            Show(Hub, Arrival.AtTownPortal);

            return;
        }

        if (Journey.TownPortal is { } spot && FindCircle(spot.CircleId) is { } circle)
            EnterCircle(circle, spot.Depth, Arrival.At(new Vector3(spot.X, 0f, spot.Z)));
    }

    private void CloseTownPortal()
    {
        Journey.CloseTownPortal();

        if (IsInstanceValid(openPortal))
            Remove(openPortal);

        openPortal = null;

        Changed?.Invoke();
    }

    //Das Portal steht neben dem Helden, zuerst wird vor ihm im Bild gesucht. Hinter einer Mauer käme niemand hindurch
    private Vector3 FindPlaceForTownPortal()
    {
        var origin = WorldScale.OnGround(Hero.GlobalPosition);
        var space  = Hero.GetWorld3D().DirectSpaceState;

        for (var step = 0; step < TownPortalDirections; step++)
        {
            var angle = Mathf.DegToRad(SouthDegrees + step * 360f / TownPortalDirections);
            var point = origin + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * TownPortalGapMeters;

            if (!Level.Layout.IsFloor(Level.Grid.GetCell(point)))
                continue;

            var sight = PhysicsRayQueryParameters3D.Create(origin + Vector3.Up * SightHeightMeters, point + Vector3.Up * SightHeightMeters, CollisionLayers.Walls);

            if (space.IntersectRay(sight).Count == 0)
                return point;
        }

        return origin;
    }

    #endregion

    #region Aussehen

    //Erst aushängen, dann freigeben: Die Navigation und die Suche nach Plätzen sähen sonst noch die alten Mauern
    private static void Remove(Node node)
    {
        node.GetParent()?.RemoveChild(node);

        node.QueueFree();
    }

    private void ApplyLook(LevelThemeResource theme)
    {
        new SurfaceLook(theme.AmbientColor, theme.AmbientEnergy, theme.FogColor, theme.MoonlightColor, theme.MoonlightEnergy).ApplyTo(Surroundings, Moonlight);

        PlayMusic(theme.Music);
    }

    private void PlayMusic(AudioStream stream)
    {
        if (stream is null)
        {
            StopMusic();

            return;
        }

        if (music?.Stream == stream && music.Playing)
            return;

        //Die Musik läuft auch im Pausenmenü und hinter dem Vorhang weiter
        if (music is null)
        {
            music = new AudioStreamPlayer { Name = "Music", Bus = AudioBuses.Music, ProcessMode = ProcessModeEnum.Always };

            AddChild(music);
        }

        music.Stream = stream;

        music.Play();
    }

    private void StopMusic()
        => music?.Stop();

    private readonly record struct SurfaceLook(Color Ambient, float AmbientEnergy, Color Fog, Color Moon, float MoonEnergy)
    {
        public static SurfaceLook From(WorldEnvironment surroundings, DirectionalLight3D moonlight)
            => new(surroundings?.Environment?.AmbientLightColor ?? Colors.White,
                   surroundings?.Environment?.AmbientLightEnergy ?? 1f,
                   surroundings?.Environment?.FogLightColor ?? Colors.Black,
                   moonlight?.LightColor ?? Colors.White,
                   moonlight?.LightEnergy ?? 0f);

        public void ApplyTo(WorldEnvironment surroundings, DirectionalLight3D moonlight)
        {
            if (surroundings?.Environment is { } environment)
            {
                environment.AmbientLightColor  = Ambient;
                environment.AmbientLightEnergy = AmbientEnergy;
                environment.FogLightColor      = Fog;
            }

            if (moonlight is not null)
            {
                moonlight.LightColor  = Moon;
                moonlight.LightEnergy = MoonEnergy;
            }
        }
    }

    #endregion
}
