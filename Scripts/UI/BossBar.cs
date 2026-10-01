using System.Linq;
using Godot;
using Hoellenspiralenspiel.Scripts.Units.Enemies;

namespace Hoellenspiralenspiel.Scripts.UI;

//Der breite Lebensbalken des Bosses oben in der Hud. Er zeigt sich, solange der Boss in Sicht ist, und verschwindet mit seinem Tod.
//Darunter steht kurz, was sich geöffnet hat
public partial class BossBar : Control
{
    private const double AnnouncementSec = 5.0;

    private Label   announcementLabel;
    private double  announcementLeft;
    private Control bar;
    private Enemy   boss;
    private Control fill;
    private float   fillWidth;
    private Label   modsLabel;
    private Label   nameLabel;

    public bool IsShown => bar?.Visible == true;

    public override void _Ready()
    {
        bar               = GetNode<Control>("%Bar");
        fill              = GetNode<Control>("%Fill");
        nameLabel         = GetNode<Label>("%NameLabel");
        modsLabel         = GetNode<Label>("%ModsLabel");
        announcementLabel = GetNode<Label>("%AnnouncementLabel");
        fillWidth         = fill.Size.X;

        bar.Visible               = false;
        announcementLabel.Visible = false;
    }

    public void Watch(Enemy enemy)
    {
        boss = enemy;

        nameLabel.Text = enemy.DisplayName;
        modsLabel.Text = string.Join(" · ", enemy.Mods.Select(mod => mod.Definition.Name));

        nameLabel.AddThemeColorOverride("font_color", enemy.NameColor);
    }

    public void Announce(string text)
    {
        announcementLabel.Text    = text;
        announcementLabel.Visible = true;
        announcementLeft          = AnnouncementSec;
    }

    public override void _Process(double delta)
    {
        var alive = boss is not null && IsInstanceValid(boss) && boss.IsInsideTree() && !boss.IsDying && !boss.IsDead;

        bar.Visible = alive && boss.IsSeen;

        if (alive)
        {
            var ratio = boss.LifeMaximum > 0 ? boss.LifeCurrent / boss.LifeMaximum : 0f;

            fill.Size = new Vector2(fillWidth * Mathf.Clamp(ratio, 0f, 1f), fill.Size.Y);
        }

        if (announcementLeft <= 0)
            return;

        announcementLeft -= delta;

        if (announcementLeft <= 0)
            announcementLabel.Visible = false;
    }
}
