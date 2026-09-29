using System.Collections.Generic;
using Godot;

namespace Hoellenspiralenspiel.Scripts.Spike3D;

//Im Vergleich liegt die 3D-Szene neben der 2D-Szene. Nach einem Wechsel stünde sie im Feld EffectScene des Skills
public static class EffectScenes3D
{
    private static readonly Dictionary<string, string> PathOfSkill = new()
    {
        ["fireball"]  = "res://Scenes/Spike3D/Skills/fireball_3d.tscn",
        ["fire_spit"] = "res://Scenes/Spike3D/Skills/fireball_3d.tscn"
    };

    private static readonly Dictionary<string, PackedScene> Loaded = new();

    public static PackedScene Find(string skillId)
    {
        if (Loaded.TryGetValue(skillId, out var scene))
            return scene;

        if (!PathOfSkill.TryGetValue(skillId, out var path))
            return null;

        scene = ResourceLoader.Load<PackedScene>(path);

        Loaded[skillId] = scene;

        return scene;
    }
}
