namespace Hoellenspiralenspiel.Scripts.Core.Levels;

//Die Gitter des Boss-Raums: zu, solange der Held lebend im Raum steht und der Boss lebt.
//Stirbt einer von beiden oder verlässt der Held den Raum, etwa durch sein Town-Portal, öffnen sie sich
public static class BossArenaRule
{
    public static bool IsSealed(bool heroInRoom, bool heroAlive, bool bossAlive)
        => heroInRoom && heroAlive && bossAlive;
}
