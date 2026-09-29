using System.Collections.Generic;
using Godot;

namespace Hoellenspiralenspiel.Scripts.Spike3D;

//Solange das 2D-Spiel läuft, zeigen Skills und Waffen auf ihre 2D-Szene. Die 3D-Szene steht deshalb hier, unter der Id von Skill oder Waffe
public static class EffectScenes3D
{
    private static readonly Dictionary<string, string> PathOfSkill = new()
    {
        ["fireball"]  = "res://Scenes/Spike3D/Skills/fireball_3d.tscn",
        ["fire_spit"] = "res://Scenes/Spike3D/Skills/fireball_3d.tscn",
        ["short_bow"] = "res://Scenes/Spike3D/Skills/arrow_3d.tscn"
    };

    private static readonly Dictionary<string, PackedScene> Loaded = new();

    public static PackedScene Find(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        if (Loaded.TryGetValue(id, out var scene))
            return scene;

        if (!PathOfSkill.TryGetValue(id, out var path))
            return null;

        scene = ResourceLoader.Load<PackedScene>(path);

        Loaded[id] = scene;

        return scene;
    }
}
