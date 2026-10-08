using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;

namespace Hoellenspiralenspiel.Scripts.Units.StatusLooks;

//Was ein Statuseffekt an der Einheit zeigt, solange er läuft. Ein Effekt ohne Szene hier zeigt nur seinen Namen über der Einheit.
//Ein neues Aussehen braucht eine Szene, deren Wurzel von StatusLook erbt, und eine Zeile in ScenePaths
public abstract partial class StatusLook : Node3D
{
    private static readonly Dictionary<StatusEffectKind, string> ScenePaths = new()
    {
        [StatusEffectKind.Brittle] = "res://Scenes/Units/StatusLooks/brittle_crust.tscn"
    };

    private static readonly Dictionary<StatusEffectKind, PackedScene> LoadedScenes = new();

    public static StatusLook Create(StatusEffectKind kind)
    {
        if (!ScenePaths.TryGetValue(kind, out var path))
            return null;

        if (!LoadedScenes.TryGetValue(kind, out var scene))
            LoadedScenes[kind] = scene = ResourceLoader.Load<PackedScene>(path);

        return scene?.Instantiate<StatusLook>();
    }

    //Nach dem Einhängen aufrufen. visual ist die Darstellung der Einheit, height und radius messen ihr sichtbares Modell in Metern
    public abstract void Attach(Node3D visual, float height, float radius);

    //Nimmt alles wieder von der Einheit und gibt sich danach selbst frei
    public abstract void Detach();
}
