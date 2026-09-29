using System;
using Godot;
using Hoellenspiralenspiel.Scripts.Objects;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.Environment;

public partial class CellarDoor
        : Area3D,
          IUsable
{
    [Export]
    public Node3D Glow { get; set; }

    //In Pixeln wie alle Reichweiten. Wer weiter entfernt steht, muss erst hinlaufen
    [Export]
    public float UseRadius { get; set; } = 150f;

    public bool IsHovered { get; private set; }

    public int TimesOpened { get; private set; }

    public event Action<CellarDoor> Opened;

    public override void _Ready()
        => SetHighlight(false);

    public bool IsInReachOf(BaseUnit unit)
        => unit.DistancePxTo(GlobalPosition) <= UseRadius;

    public void Use()
        => Open();

    public void Open()
    {
        TimesOpened++;

        Opened?.Invoke(this);
    }

    public void SetHighlight(bool active)
    {
        IsHovered = active;

        if (Glow is not null)
            Glow.Visible = active;
    }
}
