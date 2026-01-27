using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.UI.Tooltips;

public partial class AbilityTooltip : BaseTooltip
{
    private double   timeSinceLastToggle;
    private bool     tooltipIsShowing;
    public  Player2D Player { get; set; }

    public override void _Ready()
    {
        base._Ready();

        Player = GetTree()?.CurrentScene?.GetNode<Player2D>("%Player 2D");
    }
}