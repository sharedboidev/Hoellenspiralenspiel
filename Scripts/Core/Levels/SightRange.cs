using System;

namespace Hoellenspiralenspiel.Scripts.Core.Levels;

//Wie weit der Held Gegner sieht. Jenseits von RadiusMeters bleiben sie verborgen, auf den letzten EdgeMeters davor blenden sie ein.
//Dieselbe Rechnung steht im Shader ps1_unit. Wer hier etwas ändert, ändert es auch dort
public readonly record struct SightRange(float RadiusMeters, float EdgeMeters)
{
    public static SightRange Unlimited { get; } = new(0f, 0f);

    public bool IsLimited => RadiusMeters > 0f;

    //Ein Faktor von 0 hebt die Grenze auf
    public static SightRange From(float lightRadiusMeters, float radiusFactor, float edgeMeters)
    {
        var radius = lightRadiusMeters * radiusFactor;

        return radius > 0f ? new SightRange(radius, Math.Clamp(edgeMeters, 0f, radius)) : Unlimited;
    }

    //0 ist verborgen, 1 ist ganz zu sehen
    public float GetVisibility(float distanceMeters)
    {
        if (!IsLimited)
            return 1f;

        if (EdgeMeters <= 0f)
            return distanceMeters < RadiusMeters ? 1f : 0f;

        var share = Math.Clamp((distanceMeters - (RadiusMeters - EdgeMeters)) / EdgeMeters, 0f, 1f);

        return 1f - share * share * (3f - 2f * share);
    }

    //Es reicht, wenn der Rand des Körpers in die Sicht ragt
    public bool Reaches(float distanceMeters, float bodyRadiusMeters = 0f)
        => !IsLimited || distanceMeters - bodyRadiusMeters < RadiusMeters;
}
