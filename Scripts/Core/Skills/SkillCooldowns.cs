using System;
using System.Collections.Generic;

namespace Hoellenspiralenspiel.Scripts.Core.Skills;

public sealed class SkillCooldowns
{
    private readonly List<Entry> running = new();

    public bool HasAny => running.Count > 0;

    public event Action<string> Started;

    public event Action<string> Finished;

    public bool IsReady(string skillId)
        => Find(skillId) is null;

    public double GetRemainingSec(string skillId)
        => Find(skillId)?.RemainingSec ?? 0;

    public double GetTotalSec(string skillId)
        => Find(skillId)?.TotalSec ?? 0;

    public void Start(string skillId, double durationSec)
    {
        if (string.IsNullOrEmpty(skillId) || durationSec <= 0)
            return;

        var entry = Find(skillId);

        if (entry is null)
        {
            entry = new Entry(skillId);
            running.Add(entry);
        }

        entry.TotalSec     = durationSec;
        entry.RemainingSec = durationSec;

        Started?.Invoke(skillId);
    }

    public void Advance(double deltaSec)
    {
        if (deltaSec <= 0)
            return;

        for (var i = running.Count - 1; i >= 0; i--)
        {
            var entry = running[i];

            entry.RemainingSec -= deltaSec;

            if (entry.RemainingSec > 0)
                continue;

            running.RemoveAt(i);

            Finished?.Invoke(entry.SkillId);
        }
    }

    public void Clear()
    {
        for (var i = running.Count - 1; i >= 0; i--)
        {
            var entry = running[i];

            running.RemoveAt(i);

            Finished?.Invoke(entry.SkillId);
        }
    }

    private Entry Find(string skillId)
    {
        foreach (var entry in running)
        {
            if (entry.SkillId == skillId)
                return entry;
        }

        return null;
    }

    private sealed class Entry(string skillId)
    {
        public string SkillId      { get; } = skillId;
        public double TotalSec     { get; set; }
        public double RemainingSec { get; set; }
    }
}
