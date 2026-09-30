using System;
using Godot;
using Hoellenspiralenspiel.Scripts.UI;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.Objects;

//Etwas Festes an einem Ort, das der Held benutzt, ohne den Ort zu wechseln: Truhe oder Händler
public partial class Fixture
        : Area3D,
          IUsable
{
    [Export]
    public Node3D Glow { get; set; }

    //In Pixeln wie alle Reichweiten. Wer weiter entfernt steht, muss erst hinlaufen
    [Export]
    public float UseRadius { get; set; } = 200f;

    [ExportGroup("Schild")]
    [Export]
    public string DisplayName { get; set; } = string.Empty;

    [Export]
    public string Hint { get; set; } = string.Empty;

    [Export]
    public float TagHeightMeters { get; set; } = 2.2f;

    [Export]
    public Color TagColor { get; set; } = new(1f, 0.75f, 0.45f);

    public bool IsHovered { get; private set; }

    public int TimesUsed { get; private set; }

    public event Action<Fixture> Used;

    public override void _Ready()
    {
        SetHighlight(false);
        ShowTag();
    }

    public bool IsInReachOf(BaseUnit unit)
        => IsNear(unit, 0f);

    public bool IsNear(BaseUnit unit, float slackPx)
        => unit.DistancePxTo(GlobalPosition) <= UseRadius + slackPx;

    public void Use()
    {
        TimesUsed++;

        Used?.Invoke(this);
    }

    public void SetHighlight(bool active)
    {
        IsHovered = active;

        if (Glow is not null)
            Glow.Visible = active;
    }

    private void ShowTag()
    {
        if (string.IsNullOrEmpty(DisplayName))
            return;

        CombatText.GetLayer(GetTree().CurrentScene ?? GetTree().Root).AddChild(NameTag.Create(this, TagHeightMeters, DisplayName, TagColor, Hint));
    }
}
