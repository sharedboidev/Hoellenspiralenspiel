using System;
using Hoellenspiralenspiel.Scripts.Core.Combat;

namespace Hoellenspiralenspiel.Scripts.Skills;

//Die Kollisionsebenen aus den Projekteinstellungen als Bitmasken
public static class CollisionLayers
{
    public const uint Player  = 1;
    public const uint Monster = 2;
    public const uint Spells  = 4;
    public const uint Walls   = 8;
    public const uint Ground  = 16;

    public const uint Interactive = 32;

    //Ein Held mit Phasing liegt hier statt auf Player. Kein Gegner hat diese Ebene in seiner Maske, also läuft er durch sie hindurch und sie durch ihn.
    //Die Skills der Gegner treffen ihn weiter, siehe GetSkillMask
    public const uint Phasing = 64;

    public const uint NavigationSources = Ground | Walls;

    public static uint GetBodyLayer(Faction faction)
        => faction == Faction.Player ? Player : Monster;

    //Alle Ebenen, auf denen ein Körper dieser Fraktion liegen kann
    public static uint GetBodyLayers(Faction faction)
        => faction == Faction.Player ? Player | Phasing : Monster;

    public static uint GetSkillMask(Faction faction)
    {
        var mask = Walls;

        foreach (var otherFaction in Enum.GetValues<Faction>())
        {
            if (otherFaction != faction)
                mask |= GetBodyLayers(otherFaction);
        }

        return mask;
    }
}
