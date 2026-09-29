using System;
using System.Collections.Generic;

namespace Hoellenspiralenspiel.Scripts.Core.Levels;

public readonly record struct TownPortalSpot(string CircleId, int Depth, float X, float Z);

//Der Weg eines Charakters durch die Hölle: welche Kreise offen sind, wie weit er in jedem kam und wo sein Town-Portal steht
public sealed class JourneyState
{
    private readonly Dictionary<string, DescentState> descents = new();

    public int UnlockedCircles { get; private set; } = 1;

    public TownPortalSpot? TownPortal { get; private set; }

    public IReadOnlyDictionary<string, DescentState> Descents => descents;

    public DescentState GetDescent(string circleId)
    {
        ArgumentException.ThrowIfNullOrEmpty(circleId);

        if (!descents.TryGetValue(circleId, out var descent))
            descents[circleId] = descent = new DescentState();

        return descent;
    }

    public bool IsUnlocked(int circleNumber)
        => circleNumber >= 1 && circleNumber <= UnlockedCircles;

    public void Unlock(int circleNumber)
        => UnlockedCircles = Math.Max(UnlockedCircles, circleNumber);

    //Mit den Ebenen verschwindet auch das Portal, das in ihnen stand
    public void BeginAnew(string circleId, int seed)
    {
        GetDescent(circleId).Begin(seed);

        if (TownPortal?.CircleId == circleId)
            TownPortal = null;
    }

    public void OpenTownPortal(TownPortalSpot spot)
    {
        if (!string.IsNullOrEmpty(spot.CircleId) && spot.Depth >= 1)
            TownPortal = spot;
    }

    public void CloseTownPortal()
        => TownPortal = null;

    public void Reset(int unlockedCircles = 1)
    {
        descents.Clear();

        UnlockedCircles = Math.Max(1, unlockedCircles);
        TownPortal      = null;
    }
}
