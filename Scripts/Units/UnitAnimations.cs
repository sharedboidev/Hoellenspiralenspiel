using System.Linq;
using Godot;

namespace Hoellenspiralenspiel.Scripts.Units;

//Spielt die Animationen eines Modells mit Skelett. Hat das Modell keinen AnimationPlayer, bleibt die Einheit bei ihren Tweens.
//Angriffe sind zwei Animationen, damit der Treffer ohne weitere Angabe auf das Ende des Ausholens fällt
public sealed class UnitAnimations
{
    private const double BlendSec = 0.15;

    //Darunter steht die Einheit, statt in Zeitlupe zu laufen
    private const float MinWalkRate = 0.1f;

    private static readonly StringName Idle          = "Idle";
    private static readonly StringName Walk          = "Walk";
    private static readonly StringName AttackWindup  = "AttackWindup";
    private static readonly StringName AttackRecover = "AttackRecover";
    private static readonly StringName Death         = "Death";

    private readonly AnimationPlayer player;
    private          bool            isBusy;

    private UnitAnimations(AnimationPlayer player)
    {
        this.player = player;

        Play(Idle, 1f);
    }

    public bool CanAttack => player.HasAnimation(AttackWindup) && player.HasAnimation(AttackRecover);

    public bool CanDie => player.HasAnimation(Death);

    public static UnitAnimations Find(Node3D visual)
    {
        var player = visual?.FindChildren("*", nameof(AnimationPlayer), true, false).OfType<AnimationPlayer>().FirstOrDefault();

        return player is null ? null : new UnitAnimations(player);
    }

    //Wer schläft oder nicht zu sehen ist, spart sich die Animation
    public void SetRunning(bool isRunning)
        => player.Active = isRunning;

    //Bei 1 bewegt die Einheit sich so schnell, wie die Laufanimation ihre Füße setzt
    public void ShowMovement(float walkRate)
    {
        if (isBusy)
            return;

        if (walkRate < MinWalkRate)
            Play(Idle, 1f);
        else
            Play(Walk, walkRate);
    }

    public void BeginWindup(double windupSec)
    {
        isBusy = true;

        PlayWithin(AttackWindup, windupSec);
    }

    public void BeginRecovery(double recoverySec)
    {
        if (isBusy)
            PlayWithin(AttackRecover, recoverySec);
    }

    public void EndAttack()
    {
        if (!isBusy)
            return;

        isBusy = false;

        Play(Idle, 1f);
    }

    //Liefert die Dauer der Animation
    public double Die()
    {
        isBusy = true;

        Play(Death, 1f);

        return player.GetAnimation(Death).Length;
    }

    private void PlayWithin(StringName animation, double sec)
    {
        var length = player.GetAnimation(animation).Length;

        Play(animation, sec > 0 ? (float)(length / sec) : 1f);
    }

    private void Play(StringName animation, float speed)
    {
        player.SpeedScale = speed;

        if (player.CurrentAnimation != animation)
            player.Play(animation, BlendSec);
    }
}
