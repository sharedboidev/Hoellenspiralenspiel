using System.Collections.Generic;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Spatial;
using Hoellenspiralenspiel.Scripts.Objects;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.Utils;
using Hoellenspiralenspiel.Scripts.World;

namespace Hoellenspiralenspiel.Scripts.UI;

//Die Schilder der Beutel. Ein Schild weicht beim Anlegen anderen nach oben aus und behält danach seinen Platz,
//auch wenn ein Nachbar aufgehoben wird. Neu ausgerichtet wird nur beim Einschalten, beim Laufen bleiben die Schilder starr
public partial class LootLabels : Control
{
    private const float GapPx      = 4f;
    private const float LiftMeters = 0.8f;

    private readonly Dictionary<Lootbag, LootLabel> labels = new();

    private Camera3D.ProjectionType lastProjection;
    private float                   lastZoom;

    //Solange die Schilder zu sehen sind, hebt man Beutel nur über ihr Schild auf
    public static bool AreShown { get; private set; } = true;

    [Export]
    public Hero Hero { get; set; }

    public IReadOnlyCollection<LootLabel> Labels => labels.Values;

    public override void _EnterTree()
    {
        Lootbag.Appeared += OnAppeared;
        Lootbag.Vanished += OnVanished;
    }

    public override void _ExitTree()
    {
        Lootbag.Appeared -= OnAppeared;
        Lootbag.Vanished -= OnVanished;

        GetViewport().SizeChanged -= ArrangeAnew;
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;

        GetViewport().SizeChanged += ArrangeAnew;

        foreach (var lootbag in Lootbag.Lying)
            Add(lootbag);

        ArrangeAnew();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsActionPressed(InputActions.ToggleLootLabels))
            return;

        Toggle();

        GetViewport().SetInputAsHandled();
    }

    public override void _Process(double delta)
    {
        var camera = GetViewport().GetCamera3D();

        if (camera is null)
            return;

        if (HasViewChanged(camera))
            ArrangeAnew();

        foreach (var label in labels.Values)
        {
            label.Visible = (AreShown || IsBagHovered(label)) && !camera.IsPositionBehind(label.WorldAnchor);

            if (!label.Visible)
                continue;

            //Sind die Schilder aus, zeigt sich nur das des Beutels unter der Maus. Allein braucht es keinen Platz im Stapel
            if (!AreShown)
                Keep(label, GetWantedBox(label, camera), camera);

            label.Position = (camera.UnprojectPosition(label.WorldAnchor) + label.Offset).Round();
        }
    }

    public void Toggle()
    {
        AreShown = !AreShown;

        foreach (var label in labels.Values)
            label.MouseFilter = GetLabelFilter();

        if (AreShown)
        {
            ArrangeAnew();

            return;
        }

        foreach (var label in labels.Values)
        {
            label.Visible = false;

            label.ShowHovered(false);
        }
    }

    //Sind die Schilder aus, hebt man über den Beutel auf. Ein Schild, das die Maus abfängt, stünde dabei im Weg
    private static MouseFilterEnum GetLabelFilter()
        => AreShown ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;

    private static bool IsBagHovered(LootLabel label)
        => IsInstanceValid(label.Lootbag) && label.Lootbag.IsHighlighted;

    public void ArrangeAnew()
    {
        var camera = GetViewport()?.GetCamera3D();

        if (camera is null || labels.Count == 0)
            return;

        var ordered = labels.Values.ToList();
        var placed  = LabelStacker.PlaceAll(ordered.Select(label => GetWantedBox(label, camera)).ToList(), GapPx);

        for (var i = 0; i < ordered.Count; i++)
            Keep(ordered[i], placed[i], camera);
    }

    private void OnAppeared(Lootbag lootbag)
    {
        var label  = Add(lootbag);
        var camera = GetViewport().GetCamera3D();

        if (camera is null)
            return;

        var others = labels.Values.Where(other => other != label).Select(other => GetCurrentBox(other, camera)).ToList();

        Keep(label, LabelStacker.Place(GetWantedBox(label, camera), others, GapPx), camera);
    }

    private void OnVanished(Lootbag lootbag)
    {
        if (!labels.Remove(lootbag, out var label))
            return;

        label.QueueFree();
    }

    private LootLabel Add(Lootbag lootbag)
    {
        var label = LootLabel.Create(lootbag);

        label.Visible     =  false;
        label.MouseFilter =  GetLabelFilter();
        label.Clicked     += OnClicked;

        labels[lootbag] = label;

        AddChild(label);

        label.Size = label.GetCombinedMinimumSize();

        return label;
    }

    private void OnClicked(LootLabel label)
    {
        if (IsInstanceValid(Hero) && IsInstanceValid(label.Lootbag))
            Hero.OrderPickUp(label.Lootbag);
    }

    private static LabelBox GetWantedBox(LootLabel label, Camera3D camera)
    {
        var above = camera.UnprojectPosition(label.WorldAnchor + Vector3.Up * LiftMeters);

        return new LabelBox(above.X - label.Size.X / 2f, above.Y - label.Size.Y, label.Size.X, label.Size.Y);
    }

    private static LabelBox GetCurrentBox(LootLabel label, Camera3D camera)
    {
        var corner = camera.UnprojectPosition(label.WorldAnchor) + label.Offset;

        return new LabelBox(corner.X, corner.Y, label.Size.X, label.Size.Y);
    }

    private static void Keep(LootLabel label, LabelBox box, Camera3D camera)
        => label.Offset = new Vector2(box.Left, box.Top) - camera.UnprojectPosition(label.WorldAnchor);

    //Wechselt die Kamera ihre Art oder ihren Ausschnitt, rücken die Beutel auf dem Bildschirm anders zusammen.
    //Die IsoCamera zoomt perspektivisch über den Abstand, nicht über das Sichtfeld
    private bool HasViewChanged(Camera3D camera)
    {
        var zoom = camera switch
        {
            IsoCamera isoCamera                                => isoCamera.ViewHeight,
            { Projection: Camera3D.ProjectionType.Orthogonal } => camera.Size,
            _                                                  => camera.Fov
        };

        if (camera.Projection == lastProjection && Mathf.IsEqualApprox(zoom, lastZoom))
            return false;

        lastProjection = camera.Projection;
        lastZoom       = zoom;

        return true;
    }
}
