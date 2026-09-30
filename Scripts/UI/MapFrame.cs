using System.Linq;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Hud;
using Hoellenspiralenspiel.Scripts.UI.Character;

namespace Hoellenspiralenspiel.Scripts.UI;

//Die Karten füllen nur den breitesten Streifen, den offene Fenster frei lassen, und zeigen den Helden in dessen Mitte
public partial class MapFrame : Control
{
    private ScreenSpan? applied;

    [Export]
    public CharacterSheet Sheet { get; set; }

    //Ist der Streifen schmaler, bleibt die Karte weg, bis wieder Platz ist. Ob sie per Tab offen ist, bleibt dabei erhalten
    [Export]
    public float MinWidthPx { get; set; } = 400f;

    public override void _Ready()
        => Apply(FindFreeSpan());

    //Die Fenster melden nicht, wenn sie auf- oder zugehen. Gesetzt wird nur eine Änderung, sonst legt der SubViewport der Karte jedes Frame sein Bild neu an
    public override void _Process(double delta)
    {
        var span = FindFreeSpan();

        if (span != applied)
            Apply(span);
    }

    private ScreenSpan? FindFreeSpan()
    {
        var covered = Sheet?.GetCoveredRects().Select(rect => new ScreenSpan(rect.Position.X, rect.End.X)) ?? [];

        return FreeSpan.Widest(GetParentAreaSize().X, covered, MinWidthPx);
    }

    private void Apply(ScreenSpan? span)
    {
        applied = span;
        Visible = span is not null;

        if (span is not { } free)
            return;

        OffsetLeft  = free.Left;
        OffsetRight = free.Right;
    }
}
