using Hoellenspiralenspiel.Scripts.Skills;

namespace Hoellenspiralenspiel.Scripts.Spike3D;

//Die Körper benutzen dieselben Bits wie in 2D, der Boden kommt für das Navigationsnetz dazu
public static class Layers3D
{
    public const uint Ground = 16;

    public const uint NavigationSources = Ground | CollisionLayers.Walls;
}
