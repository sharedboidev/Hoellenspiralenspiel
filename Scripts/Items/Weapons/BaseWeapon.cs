using Godot;

namespace Hoellenspiralenspiel.Scripts.Items.Weapons;

public abstract partial class BaseWeapon : BaseItem
{
    [Export]
    public int MinDamage { get; set; }

    [Export]
    public int MaxDamage { get; set; }

    [Export]
    public float SwingtimerSec { get; set; } = 1f;

    public float AttacksPerSecond => 1 / SwingtimerSec;

    public override bool IsStackable => false;
}