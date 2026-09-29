using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Levels;

namespace Hoellenspiralenspiel.Scripts.World.Levels;

//Liest aus den Raumszenen, was der Generator wissen muss: Größe, Türen und Regeln
public sealed class RoomLibrary
{
    private readonly List<RoomBlueprint>             blueprints = new();
    private readonly Dictionary<string, PackedScene> sceneOf    = new();

    public RoomLibrary(IEnumerable<PackedScene> scenes)
    {
        foreach (var scene in scenes)
        {
            if (scene is null)
                continue;

            var id = scene.ResourcePath.GetFile().GetBaseName();

            if (sceneOf.ContainsKey(id))
            {
                GD.PushWarning($"Die Raumvorlage {id} steht doppelt im Thema.");

                continue;
            }

            var room = scene.Instantiate();

            if (room is RoomTemplate template)
            {
                blueprints.Add(template.ToBlueprint(id));

                sceneOf[id] = scene;
            }
            else
                GD.PushWarning($"Die Szene {scene.ResourcePath} ist keine Raumvorlage, an ihrer Wurzel fehlt das Skript {nameof(RoomTemplate)}.");

            room.Free();
        }
    }

    public IReadOnlyList<RoomBlueprint> Blueprints => blueprints;

    public RoomTemplate Create(RoomBlueprint blueprint)
        => sceneOf[blueprint.Id].Instantiate<RoomTemplate>();
}
