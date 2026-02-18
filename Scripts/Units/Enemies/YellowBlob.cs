using System;
using Godot;

namespace Hoellenspiralenspiel.Scripts.Units.Enemies;

public partial class YellowBlob : BaseEnemy
{
    protected override PackedScene AttackScene { get; }

    protected override void ExecuteAttack() => throw new NotImplementedException();
}