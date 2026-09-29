using System;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.Core.Stats;

namespace Hoellenspiralenspiel.Interfaces;

//Was Charakterbogen und Inventar vom Helden brauchen, egal ob er in 2D oder 3D steht
public interface IHero
{
    StatSheet Stats { get; }

    CharacterItems Items { get; }

    int Level { get; }

    event Action SheetChanged;

    void Consume(ItemInstance item);
}
