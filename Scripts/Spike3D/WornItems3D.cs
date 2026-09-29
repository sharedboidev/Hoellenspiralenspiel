using System.Collections.Generic;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Resources.Items;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.Items;

namespace Hoellenspiralenspiel.Scripts.Spike3D;

//Ein Platz ist sichtbar, wenn das Modell des Trägers einen Knoten namens Attach plus Platz hat, etwa AttachHelmet.
//Paarige Plätze haben mehrere davon, etwa AttachHandsLeft und AttachHandsRight, und bekommen das Modell an jedem
public sealed class WornItems3D
{
    private const string AttachPrefix = "Attach";

    private readonly Node3D                           body;
    private readonly Dictionary<ItemSlot, List<Node>> modelsOfPlace = new();

    public WornItems3D(Node3D body)
        => this.body = body;

    public IReadOnlyCollection<ItemSlot> ShownPlaces => modelsOfPlace.Keys;

    public void ShowAll(Equipment equipment)
    {
        foreach (var item in equipment.Items.Values)
            Show(item);
    }

    public void Show(ItemInstance item)
    {
        var place = Equipment.GetPlaceFor(item.Definition.Slot);

        Hide(place);

        if (ItemLibrary.Find(item.Definition.Id) is not EquippableBaseResource { WornModel: not null } itemBase)
            return;

        var models = new List<Node>();

        foreach (var point in FindAttachPoints(place))
        {
            var model = itemBase.WornModel.Instantiate();

            point.AddChild(model);
            models.Add(model);
        }

        if (models.Count > 0)
            modelsOfPlace[place] = models;
    }

    public void Hide(ItemInstance item)
        => Hide(Equipment.GetPlaceFor(item.Definition.Slot));

    public void Hide(ItemSlot place)
    {
        if (!modelsOfPlace.Remove(place, out var models))
            return;

        foreach (var model in models.Where(GodotObject.IsInstanceValid))
            model.QueueFree();
    }

    public IEnumerable<Node3D> FindAttachPoints(ItemSlot place)
        => body.FindChildren($"{AttachPrefix}{place}*", nameof(Node3D), true, false).OfType<Node3D>();
}
