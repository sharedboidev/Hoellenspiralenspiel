namespace Hoellenspiralenspiel.Scripts.Core.Combat;

//Als Struct vergleicht sich eine Spanne nach ihrem Inhalt, so bleiben Waffe, Treffer und Ergebnis als Records vergleichbar
public readonly record struct DamageRange(float Min, float Max)
{
    public bool IsEmpty => Max <= 0f;

    public DamageRange Times(float factor)
        => new(Min * factor, Max * factor);

    public static DamageRange operator +(DamageRange left, DamageRange right)
        => new(left.Min + right.Min, left.Max + right.Max);
}
