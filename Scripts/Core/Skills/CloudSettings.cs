using System;
using Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;

namespace Hoellenspiralenspiel.Scripts.Core.Skills;

//Ein Nebel am Boden: Er liegt DurationSec lang, sein Radius wächst bis dahin gleichmäßig um GrowthPercent.
//Wer ihn berührt, bekommt Effect und alle PulseSec erneut, solange er darin steht
public sealed record CloudSettings(float Radius, float DurationSec, float GrowthPercent, float PulseSec, StatusEffectKind Effect)
{
    public float GetRadiusAfter(double activeSec)
    {
        var progress = DurationSec > 0f ? Math.Clamp(activeSec / DurationSec, 0.0, 1.0) : 1.0;

        return Math.Max(0f, Radius) * (1f + Math.Max(0f, GrowthPercent) / 100f * (float)progress);
    }

    public bool IsOver(double activeSec)
        => activeSec >= DurationSec;

    //Wer schon dran war, bekommt den Effekt erst nach einem ganzen Puls wieder. Ein vermiedener Effekt zählt auch als dran
    public bool IsDue(double activeSec, double lastPulseSec)
        => activeSec - lastPulseSec >= PulseSec - 0.00001;
}
