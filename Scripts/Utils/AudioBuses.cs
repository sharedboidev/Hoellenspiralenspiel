using Godot;

namespace Hoellenspiralenspiel.Scripts.Utils;

//Namen der Busse aus default_bus_layout.tres. Jeder Ton spielt auf Musik oder Effekte, nie direkt auf Master
public static class AudioBuses
{
    public static readonly StringName Master  = "Master";
    public static readonly StringName Music   = "Music";
    public static readonly StringName Effects = "Effects";
}
