using Godot;
using Hoellenspiralenspiel.Interfaces;
using Hoellenspiralenspiel.Scripts.Core.Hud;

namespace Hoellenspiralenspiel.Scripts.UI.Tooltips;

//Mit gehaltener Umschalttaste zeigt ein Begleiter links daneben, was an diesem Platz getragen wird
public partial class ItemTooltip : BaseTooltip
{
    private Vector2                 arrangedCompanionMinimum;
    private Rect2                   arrangedRect;
    private ItemTooltip             companion;
    private bool                    isCompanion;
    private bool                    isComparing;
    private ITooltipObjectContainer shownContainer;
    private ITooltipObject          wornCounterpart;

    [Export]
    public float CompanionGapPx { get; set; } = 8;

    //Die Größe eines Tooltips stimmt manchmal erst nach dem Layout im nächsten Frame
    private bool IsArrangementOutdated => GetGlobalRect() != arrangedRect || companion.GetCombinedMinimumSize() != arrangedCompanionMinimum;

    public override void _Ready()
    {
        if (isCompanion)
            return;

        companion             = GD.Load<PackedScene>(SceneFilePath).Instantiate<ItemTooltip>();
        companion.isCompanion = true;
        companion.Visible     = false;
        companion.TopLevel    = true;
        //Absolut wie der Tooltip selbst, damit er unter dem Item an der Maus (z 15) bleibt
        companion.ZAsRelative = false;
        companion.ZIndex      = ZIndex;

        AddChild(companion);
    }

    public override void _Process(double delta)
    {
        if (shownContainer is null)
            return;

        var isShiftHeld = Input.IsKeyPressed(Key.Shift);

        if (isShiftHeld != isComparing)
            Compare(isShiftHeld);
        else if (companion.Visible && IsArrangementOutdated)
            Arrange();
    }

    public override void Show(ITooltipObjectContainer objectContainer)
    {
        base.Show(objectContainer);

        if (companion is null || objectContainer is null)
            return;

        shownContainer  = objectContainer;
        wornCounterpart = (objectContainer as InventoryItem)?.WornCounterpart;

        Compare(Input.IsKeyPressed(Key.Shift));
    }

    public override void Hide()
    {
        base.Hide();

        shownContainer  = null;
        wornCounterpart = null;

        companion?.Hide();
    }

    private void Compare(bool isShiftHeld)
    {
        isComparing       = isShiftHeld;
        companion.Visible = isComparing && wornCounterpart is not null;

        if (companion.Visible)
            companion.Fill(wornCounterpart);

        Arrange();
    }

    private void Arrange()
    {
        if (companion.Visible)
        {
            companion.ResetSize();

            PlaceWithCompanion();
        }
        else
            PlaceBy(shownContainer);

        arrangedRect             = GetGlobalRect();
        arrangedCompanionMinimum = companion.GetCombinedMinimumSize();
    }

    private void PlaceWithCompanion()
    {
        var screen = GetViewportRect().Size;
        var (main, worn) = TooltipPlacement.PlaceWithCompanion(ToBox(shownContainer), Size.X, Size.Y, companion.Size.X, companion.Size.Y, screen.X, screen.Y, CompanionGapPx);

        GlobalPosition           = new Vector2(main.X, main.Y);
        companion.GlobalPosition = new Vector2(worn.X, worn.Y);
    }
}
