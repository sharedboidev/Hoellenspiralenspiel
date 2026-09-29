using System;
using Godot;
using Hoellenspiralenspiel.Scripts.Objects;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.Environment;

//Ein Durchgang an einen anderen Ort: Treppe, Kellertür oder Portal
public partial class Passage
        : Area3D,
          IUsable
{
    private const string ArrivalName = "Arrival";

    private static readonly Vector3 ArrivalOffset = new(0f, 0f, 1.8f);

    [Export]
    public Node3D Glow { get; set; }

    //In Pixeln wie alle Reichweiten. Wer weiter entfernt steht, muss erst hinlaufen
    [Export]
    public float UseRadius { get; set; } = 150f;

    public bool IsHovered { get; private set; }

    public int TimesUsed { get; private set; }

    public virtual bool IsOpen => true;

    //Hier steht, wer durch den Durchgang ankommt
    public Vector3 ArrivalPoint => GetNodeOrNull<Node3D>(ArrivalName)?.GlobalPosition ?? GlobalPosition + GlobalBasis * ArrivalOffset;

    public event Action<Passage> Used;

    public override void _Ready()
        => SetHighlight(false);

    public bool IsInReachOf(BaseUnit unit)
        => unit.DistancePxTo(GlobalPosition) <= UseRadius;

    public void Use()
    {
        if (!IsOpen)
            return;

        TimesUsed++;

        Used?.Invoke(this);
    }

    public void SetHighlight(bool active)
    {
        IsHovered = active && IsOpen;

        if (Glow is not null)
            Glow.Visible = IsHovered;
    }
}
