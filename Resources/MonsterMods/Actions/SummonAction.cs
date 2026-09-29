using Godot;
using Hoellenspiralenspiel.Resources.Enemies;
using Hoellenspiralenspiel.Scripts.Controllers;

namespace Hoellenspiralenspiel.Resources.MonsterMods.Actions;

[GlobalClass]
public partial class SummonAction : ModAction
{
    //Leer bedeutet: dieselbe Art wie das Monster selbst
    [Export]
    public EnemyResource Enemy { get; set; }

    [Export]
    public int Count { get; set; } = 3;

    [Export]
    public float Radius { get; set; } = 120f;

    public override void Run(ModContext context)
    {
        var owner      = context.Owner;
        var definition = Enemy ?? owner.Definition;

        if (definition is null || owner.Controller is null)
            return;

        for (var i = 0; i < Count; i++)
        {
            var summoned = owner.Controller.Spawn(definition, new SpawnArea(owner.GlobalPosition, Radius), owner.SpawnGroup, owner.Level);

            if (owner.IsInCombat)
                summoned.Provoke();
        }
    }
}
