using Godot;

namespace Hoellenspiralenspiel.Scripts.UI.Character;

public partial class LevelUpEffect : GpuParticles2D
{
    public void Emit()
    {
        // var scaleTween = CreateTween();
        // scaleTween.SetEase(Tween.EaseType.Out);
        // scaleTween.SetTrans(Tween.TransitionType.Back);
        // scaleTween.TweenProperty(this, "scale", new Vector2(1.8f, 1.8f), 0.1f);
        // scaleTween.TweenProperty(this, "scale", new Vector2(0.5f, 1.5f), 0.1f);

        if (!Emitting)
        {
            Restart();
            Emitting = true;
        }
    }
}