using System;

namespace Hoellenspiralenspiel.Scripts.Core.Levels;

//Wer den Boss auf der letzten Ebene eines Kreises besiegt, schaltet den nächsten Kreis frei
public static class CircleUnlockRule
{
    //0, wenn es nichts freizuschalten gibt: Der Boss stand nicht auf der letzten Ebene, oder der Kreis war der letzte
    public static int NextCircle(int circleNumber, int depth, int levelCount)
    {
        if (depth < Math.Max(1, levelCount) || circleNumber < 1 || circleNumber >= JourneyState.LastCircle)
            return 0;

        return circleNumber + 1;
    }
}
