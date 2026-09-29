using Godot;

namespace Hoellenspiralenspiel.Scripts.Spike3D;

//Der Kern und alle Resources rechnen in Pixeln, die 3D-Welt in Metern
public static class WorldScale
{
    public const float PxPerMeter = 100f;

    public static float ToMeters(float px)
        => px / PxPerMeter;

    public static float ToPx(float meters)
        => meters * PxPerMeter;

    public static Vector3 OnGround(Vector3 vector)
        => new(vector.X, 0f, vector.Z);

    public static float GroundDistancePx(Vector3 from, Vector3 to)
        => ToPx(OnGround(to - from).Length());
}
