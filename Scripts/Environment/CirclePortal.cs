using Godot;
using Hoellenspiralenspiel.Resources.Levels;
using Hoellenspiralenspiel.Scripts.UI;

namespace Hoellenspiralenspiel.Scripts.Environment;

//Steht im Hub und führt in einen Höllenkreis. Ein gesperrtes Portal bleibt dunkel
public partial class CirclePortal : Passage
{
    private const float TagHeightMeters = 3.7f;

    private static readonly Color OpenColor   = new(1f, 0.75f, 0.45f);
    private static readonly Color SealedColor = new(0.55f, 0.55f, 0.55f);

    [Export]
    private bool    isUnlocked;
    private NameTag tag;

    //0 ist das Portal des Testkreises, es steht außerhalb der Kette der neun Kreise
    [Export(PropertyHint.Range, "0,9,1")]
    public int Number { get; set; } = 1;

    [Export]
    public Node3D OpenLook { get; set; }

    //Den Kreis zu seiner Nummer nennt der Abstieg. Fehlt er, gibt es den Kreis noch nicht
    public LevelThemeResource Circle { get; set; }

    public override bool IsOpen => isUnlocked && Circle is not null;

    protected override bool IsOutlined => IsOpen;

    public void SetUnlocked(bool unlocked)
    {
        isUnlocked = unlocked;

        if (OpenLook is not null)
            OpenLook.Visible = IsOpen;

        if (!IsOpen)
            SetHighlight(false);

        ShowOutline();
        ShowTag();
    }

    private void ShowTag()
    {
        if (IsInstanceValid(tag))
            tag.QueueFree();

        if (!IsInsideTree())
            return;

        var title = Number == 0 ? "Test circle" : $"Circle {Number}";

        tag = IsOpen
                      ? NameTag.Create(this, TagHeightMeters, Circle.DisplayName, OpenColor, title)
                      : NameTag.Create(this, TagHeightMeters, title, SealedColor, isUnlocked ? "not yet built" : "sealed");

        CombatText.GetLayer(GetTree().CurrentScene ?? GetTree().Root).AddChild(tag);
    }
}
