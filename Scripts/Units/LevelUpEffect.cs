using Godot;

namespace Hoellenspiralenspiel.Scripts.Units;

public partial class LevelUpEffect : GpuParticles3D
{
    public void Emit()
    {
        if (Emitting)
            return;

        Restart();

        Emitting = true;
    }
}
