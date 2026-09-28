namespace Hoellenspiralenspiel.Scripts.Core.Items;

public sealed record GuardStats(float MeleeBlock = 0f, float SpellBlock = 0f, float MeleeParry = 0f, float SpellParry = 0f)
{
    public static GuardStats None { get; } = new();

    public bool HasAny => MeleeBlock > 0f || SpellBlock > 0f || MeleeParry > 0f || SpellParry > 0f;
}
