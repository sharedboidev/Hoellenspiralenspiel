using System.Threading.Tasks;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Loading;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Utils;

namespace Hoellenspiralenspiel.Scripts.UI;

//Verdeckt das Bild, solange ein Ort abgebaut und der nächste aufgebaut wird. Dabei zeigt er das Ziel und einen Tipp
public partial class Curtain : ColorRect
{
    private static readonly SeededRandom TipRandom = new(System.Environment.TickCount);

    //Was der Vorhang vor einem Szenenwechsel zeigte, zeigt der Vorhang der nächsten Szene weiter
    private static string handedTip;
    private static ulong? handedDownSinceMsec;
    private static string lastTip;

    private Label  detailLabel;
    private ulong  downSinceMsec;
    private Tween  fade;
    private Stage  stage = Stage.Up;
    private Label  tipLabel;
    private Label  titleLabel;

    private enum Stage
    {
        Up,
        Dropping,
        Down,
        Lifting
    }

    [Export]
    public double FadeOutSec { get; set; } = 0.25;

    [Export]
    public double FadeInSec { get; set; } = 0.35;

    //Gezählt ab ganz schwarz, das Laden zählt mit. Dauert es länger, steht der Vorhang eben länger
    [Export]
    public double MinimumShowSec { get; set; } = 1.5;

    //In der Spielszene steht der Vorhang schon beim Start und fällt erst, wenn der Held im Hub ankommt
    [Export]
    public bool StartsDown { get; set; }

    [Export]
    public bool ShowsTips { get; set; } = true;

    //{aktion} steht für die Taste der Aktion, etwa {open_town_portal}. Ein Tipp zu einer Aktion ohne Taste entfällt
    [Export]
    public string[] Tips { get; set; } = [];

    public bool IsDown => stage == Stage.Down;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        titleLabel  = GetNode<Label>("%TitleLabel");
        detailLabel = GetNode<Label>("%DetailLabel");
        tipLabel    = GetNode<Label>("%TipLabel");

        ShowTarget(string.Empty, string.Empty);

        if (!StartsDown)
        {
            MouseFilter = MouseFilterEnum.Ignore;

            Hide();

            return;
        }

        ShowTip(handedTip ?? PickTip());
        BeDown();

        downSinceMsec = handedDownSinceMsec ?? downSinceMsec;

        handedTip           = null;
        handedDownSinceMsec = null;
    }

    //Steht der Vorhang schon, bleiben Tipp und Mindestdauer, nur das Ziel ändert sich
    public void Drop(string title = "", string detail = "")
    {
        ShowTarget(title, detail);

        MouseFilter = MouseFilterEnum.Stop;

        if (stage is Stage.Dropping or Stage.Down)
            return;

        fade?.Kill();

        var shownShare = stage == Stage.Lifting ? Modulate.A : 0f;

        ShowTip(PickTip());

        Modulate = new Color(1f, 1f, 1f, shownShare);
        stage    = Stage.Dropping;

        Show();

        var remainingSec = FadeOutSec * (1f - shownShare);

        if (remainingSec <= 0)
        {
            BeDown();

            return;
        }

        fade = CreateTween();

        fade.TweenProperty(this, "modulate:a", 1f, remainingSec);
        fade.TweenCallback(Callable.From(BeDown));
    }

    //Kehrt erst zurück, wenn das schwarze Bild einmal gezeichnet ist. Zwei Frames, weil eine Reise mitten in der Physik beginnen kann
    public async Task WaitUntilShown()
    {
        while (stage == Stage.Dropping)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    //Gezählt wird mit der Uhr, nicht mit einem Timer des Baums. Der zählte nach langen Frames zu schnell
    public async Task WaitForMinimum()
    {
        while (stage == Stage.Down && LoadingTips.RemainingSec(MinimumShowSec, (Time.GetTicksMsec() - downSinceMsec) / 1000.0) > 0)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    public void Lift()
    {
        if (stage is Stage.Up or Stage.Lifting)
            return;

        fade?.Kill();

        stage       = Stage.Lifting;
        MouseFilter = MouseFilterEnum.Ignore;

        if (FadeInSec <= 0)
        {
            BeUp();

            return;
        }

        fade = CreateTween();

        fade.TweenProperty(this, "modulate:a", 0f, FadeInSec);
        fade.TweenCallback(Callable.From(BeUp));
    }

    //Vor einem Szenenwechsel: Der Vorhang der nächsten Szene zeigt denselben Tipp und rechnet die Mindestdauer weiter
    public void HandOver()
    {
        if (stage != Stage.Down)
            return;

        handedTip           = tipLabel.Text;
        handedDownSinceMsec = downSinceMsec;
    }

    private void BeDown()
    {
        Modulate      = Colors.White;
        stage         = Stage.Down;
        MouseFilter   = MouseFilterEnum.Stop;
        downSinceMsec = Time.GetTicksMsec();

        Show();
    }

    private void BeUp()
    {
        stage = Stage.Up;

        Hide();
    }

    private string PickTip()
    {
        if (!ShowsTips)
            return null;

        var tip = LoadingTips.Pick(Tips, action => InputActions.GetKeyLabel(action), lastTip, TipRandom);

        lastTip = tip ?? lastTip;

        return tip;
    }

    private void ShowTarget(string title, string detail)
    {
        titleLabel.Text     = title ?? string.Empty;
        titleLabel.Visible  = !string.IsNullOrEmpty(title);
        detailLabel.Text    = detail ?? string.Empty;
        detailLabel.Visible = !string.IsNullOrEmpty(detail);
    }

    private void ShowTip(string tip)
    {
        tipLabel.Text    = tip ?? string.Empty;
        tipLabel.Visible = !string.IsNullOrEmpty(tip);
    }
}
