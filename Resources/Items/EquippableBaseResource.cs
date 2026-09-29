using Godot;
using Hoellenspiralenspiel.Scripts.Core.Items;

namespace Hoellenspiralenspiel.Resources.Items;

[GlobalClass]
public abstract partial class EquippableBaseResource : ItemBaseResource
{
    [ExportGroup("Parry and Block")]
    [Export(PropertyHint.Range, "0,100,0.1")]
    public float MeleeBlock { get; set; }

    [Export(PropertyHint.Range, "0,100,0.1")]
    public float SpellBlock { get; set; }

    [Export(PropertyHint.Range, "0,100,0.1")]
    public float MeleeParry { get; set; }

    [Export(PropertyHint.Range, "0,100,0.1")]
    public float SpellParry { get; set; }

    //Das Modell, das der Träger in 3D am Körper hat. Ohne Modell bleibt das Item unsichtbar
    [ExportGroup("Look")]
    [Export]
    public PackedScene WornModel { get; set; }

    protected GuardStats Guard => new(MeleeBlock, SpellBlock, MeleeParry, SpellParry);
}
