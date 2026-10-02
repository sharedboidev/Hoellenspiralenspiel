using System;
using Hoellenspiralenspiel.Enums;

namespace Hoellenspiralenspiel.Scripts.Core.Stats;

//Die Grundwerte eines neuen Helden, bevor Ausrüstung, Level und verteilte Punkte dazukommen
public sealed record HeroBaseValues
{
    public int Strength     { get; init; } = 1;
    public int Dexterity    { get; init; } = 1;
    public int Intelligence { get; init; } = 1;
    public int Constitution { get; init; } = 1;
    public int Awareness    { get; init; } = 1;

    public int   LifeBonus        { get; init; } = 50;
    public float Movementspeed    { get; init; } = 1000f;
    public float Manaregeneration { get; init; } = 0.5f;
    public float Dodge            { get; init; } = 6f;

    //In Prozent des Lichts, das die Szene dem Helden mitgibt
    public float LightRadius { get; init; } = 100f;

    public void Apply(StatSheet sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);

        sheet.Update(stats =>
        {
            stats.SetBase(CombatStat.Strength, Strength);
            stats.SetBase(CombatStat.Dexterity, Dexterity);
            stats.SetBase(CombatStat.Intelligence, Intelligence);
            stats.SetBase(CombatStat.Constitution, Constitution);
            stats.SetBase(CombatStat.Awareness, Awareness);
            stats.SetBase(CombatStat.Dodge, Dodge);
            stats.SetBase(CombatStat.Life, LifeBonus);
            stats.SetBase(CombatStat.Movementspeed, Movementspeed);
            stats.SetBase(CombatStat.Manaregeneration, Manaregeneration);
            stats.SetBase(CombatStat.LightRadius, LightRadius);
        });
    }
}
