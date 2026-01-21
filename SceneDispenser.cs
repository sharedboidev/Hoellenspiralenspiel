using Godot;

namespace Hoellenspiralenspiel;

public static class SceneDispenser
{
    private static PackedScene LoadScene(string scenePath) => ResourceLoader.Load<PackedScene>(scenePath);
}