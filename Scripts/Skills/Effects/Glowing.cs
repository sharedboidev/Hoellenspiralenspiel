using Godot;

namespace Hoellenspiralenspiel.Scripts.Skills.Effects;

//Ein leuchtendes Netz wächst, wird heller und wechselt die Farbe. Das Material der Szene teilen sich alle Instanzen, deshalb bekommt das Netz eine eigene Kopie
public static class Glowing
{
    private static readonly StringName Emission       = "emission";
    private static readonly StringName EmissionEnergy = "emission_energy";

    public static void Apply(MeshInstance3D mesh, float scale, float emissionEnergy, Color? emission = null)
    {
        mesh.Scale = Vector3.One * scale;

        if (mesh.GetSurfaceOverrideMaterial(0) is not ShaderMaterial material)
            return;

        if (!material.ResourceLocalToScene)
        {
            material = (ShaderMaterial)material.Duplicate();

            material.ResourceLocalToScene = true;

            mesh.SetSurfaceOverrideMaterial(0, material);
        }

        material.SetShaderParameter(EmissionEnergy, emissionEnergy);

        if (emission is { } color)
            material.SetShaderParameter(Emission, color);
    }

    //Die Farbe der Ladung: Der Verlauf läuft von der Mindestladung (0) bis zum Maximum (1). Ohne Verlauf bleibt die Farbe der Szene
    public static Color? Sample(Gradient colors, float share)
        => colors?.Sample(Mathf.Clamp(share, 0f, 1f));
}
