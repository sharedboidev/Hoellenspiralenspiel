using Godot;
using Hoellenspiralenspiel.Scripts.Utils;
using Hoellenspiralenspiel.Scripts.World.Levels;

namespace Hoellenspiralenspiel.Scripts.UI;

//Zeigt unter der Erde die Taste für das Town-Portal und seine Abklingzeit
public partial class TownPortalBadge : Label
{
    [Export]
    public Descent Descent { get; set; }

    [Export]
    public Color ReadyColor { get; set; } = new(0.6f, 0.75f, 1f);

    [Export]
    public Color WaitingColor { get; set; } = new(0.55f, 0.55f, 0.55f);

    public override void _Process(double delta)
    {
        Visible = Descent?.Level is not null;

        if (!Visible)
            return;

        var secLeft = Descent.TownPortalCooldownLeftSec;

        Text     = secLeft > 0 ? $"Town Portal · {Mathf.CeilToInt(secLeft)} s" : $"Town Portal [{InputActions.GetKeyLabel(InputActions.OpenTownPortal)}]";
        Modulate = secLeft > 0 ? WaitingColor : ReadyColor;
    }
}
