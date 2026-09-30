using System;
using System.Collections.Generic;
using System.Linq;

namespace Hoellenspiralenspiel.Scripts.Core.Settings;

public enum BindingKind
{
    Key,
    Mouse
}

//Eine Taste nach ihrer Lage auf der Tastatur oder eine Maustaste, als Code von Godot
public readonly record struct InputBinding(BindingKind Kind, long Code)
{
    public bool IsValid => Code > 0 && Enum.IsDefined(Kind);
}

public enum BindingOutcome
{
    Unchanged,
    Bound,
    Swapped,
    Reserved,
    MouseOnlyForSkills,
    SwapImpossible,
    NotRebindable
}

public readonly record struct BindingResult(BindingOutcome Outcome, string OtherAction = null)
{
    public bool IsAccepted => Outcome is BindingOutcome.Unchanged or BindingOutcome.Bound or BindingOutcome.Swapped;
}

//Regeln der Belegung: eine Taste je Aktion, keine Kombinationen, Maustasten nur für Skill-Plätze, bei Konflikt tauschen die Aktionen.
//Die Codes sind aus Godot.Key und Godot.MouseButton gespiegelt, der Kern kennt Godot nicht
public static class BindingRules
{
    public const string PauseAction      = "toggle_pause_menu";
    public const string SkillSlotPrefix  = "skill_slot_";

    public const long Escape = 4194305;

    //F1 bis F6 sind Tasten zum Testen
    public static readonly long[] ReservedKeys = [Escape, 4194332, 4194333, 4194334, 4194335, 4194336, 4194337];

    //Links, rechts, Mitte und die beiden Seitentasten. Das Mausrad zoomt die Kamera und kennt kein Halten
    public static readonly long[] AllowedMouseButtons = [1, 2, 3, 8, 9];

    public static bool IsSkillSlot(string action)
        => action?.StartsWith(SkillSlotPrefix, StringComparison.Ordinal) == true;

    public static bool IsRebindable(string action)
        => !string.IsNullOrEmpty(action) && action != PauseAction && !action.StartsWith("ui_", StringComparison.Ordinal);

    public static BindingOutcome Check(string action, InputBinding binding)
    {
        if (!IsRebindable(action))
            return BindingOutcome.NotRebindable;

        if (!binding.IsValid)
            return BindingOutcome.Reserved;

        return binding.Kind switch
        {
            BindingKind.Key when ReservedKeys.Contains(binding.Code)                     => BindingOutcome.Reserved,
            BindingKind.Mouse when !AllowedMouseButtons.Contains(binding.Code)           => BindingOutcome.Reserved,
            BindingKind.Mouse when !IsSkillSlot(action)                                  => BindingOutcome.MouseOnlyForSkills,
            _                                                                            => BindingOutcome.Bound
        };
    }

    //Belegt eine Aktion. Hält eine andere Aktion die Taste schon, bekommt sie die bisherige Taste dieser Aktion
    public static BindingResult Assign(IDictionary<string, InputBinding> bindings, string action, InputBinding binding)
    {
        var check = Check(action, binding);

        if (check != BindingOutcome.Bound)
            return new BindingResult(check);

        if (bindings.TryGetValue(action, out var current) && current == binding)
            return new BindingResult(BindingOutcome.Unchanged);

        var other = bindings.FirstOrDefault(pair => pair.Key != action && pair.Value == binding).Key;

        if (other is null)
        {
            bindings[action] = binding;

            return new BindingResult(BindingOutcome.Bound);
        }

        var hasCurrent = bindings.TryGetValue(action, out current);

        if (!hasCurrent || Check(other, current) != BindingOutcome.Bound)
            return new BindingResult(BindingOutcome.SwapImpossible, other);

        bindings[other]  = current;
        bindings[action] = binding;

        return new BindingResult(BindingOutcome.Swapped, other);
    }

    //Die Standardbelegung mit den Abweichungen des Spielers darüber. Die Abweichungen beschreiben den letzten Stand vollständig,
    //sie werden deshalb gesetzt und nicht als Tausch nachgespielt. Unbekannte, ungültige oder doppelte Abweichungen fallen weg
    public static Dictionary<string, InputBinding> Resolve(IReadOnlyDictionary<string, InputBinding> defaults, IReadOnlyDictionary<string, InputBinding> overrides)
    {
        var effective  = defaults.ToDictionary(pair => pair.Key, pair => pair.Value);
        var overridden = new HashSet<string>();

        foreach (var (action, binding) in (overrides ?? new Dictionary<string, InputBinding>()).OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            if (!effective.ContainsKey(action) || Check(action, binding) != BindingOutcome.Bound)
                continue;

            if (overridden.Any(other => effective[other] == binding))
                continue;

            effective[action] = binding;

            overridden.Add(action);
        }

        //Hat sich ein Standard geändert, kann er auf eine Taste des Spielers fallen. Die Aktion bekommt dann, was die andere im Standard hatte
        foreach (var action in effective.Keys.Where(action => !overridden.Contains(action)).ToList())
        {
            var holder = overridden.FirstOrDefault(other => effective[other] == effective[action]);

            if (holder is null)
                continue;

            var replacement = defaults[holder];

            if (Check(action, replacement) == BindingOutcome.Bound && effective.All(pair => pair.Key == action || pair.Value != replacement))
                effective[action] = replacement;
        }

        return effective;
    }

    public static Dictionary<string, InputBinding> Overrides(IReadOnlyDictionary<string, InputBinding> defaults, IReadOnlyDictionary<string, InputBinding> effective)
        => effective.Where(pair => !defaults.TryGetValue(pair.Key, out var standard) || standard != pair.Value)
                    .ToDictionary(pair => pair.Key, pair => pair.Value);
}
