using Godot;

namespace Hoellenspiralenspiel.Scripts.Spike3D;

public partial class LevelUpEffect3D : GpuParticles3D
{
    public void Emit()
    {
        if (Emitting)
            return;

        Restart();

        Emitting = true;
    }
}
