using System;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.Objects;

namespace Hoellenspiralenspiel.Scripts.UI;

public partial class LootLabel : PanelContainer
{
    private const int FontSize      = 20;
    private const int PaddingWidth  = 10;
    private const int PaddingHeight = 2;

    private static readonly Color BoxColor        = new(0f, 0f, 0f, 0.72f);
    private static readonly Color HoveredBoxColor = new(0.22f, 0.2f, 0.18f, 0.9f);

    private StyleBoxFlat box;

    public Lootbag Lootbag { get; private set; }

    //Der Ort des Beutels beim Anlegen. Hüpft der Beutel bei vollem Inventar, bleibt sein Schild stehen
    public Vector3 WorldAnchor { get; private set; }

    //Abstand zum Beutel auf dem Bildschirm. Steht er einmal fest, ändert ihn nur ein neues Ausrichten
    public Vector2 Offset { get; set; }

    public event Action<LootLabel> Clicked;

    public static LootLabel Create(Lootbag lootbag)
    {
        var item  = lootbag.ContainedItem;
        var label = new LootLabel
        {
            Name        = "LootLabel",
            Lootbag     = lootbag,
            WorldAnchor = lootbag.GlobalPosition,
            MouseFilter = MouseFilterEnum.Stop,
            box         = CreateBox()
        };

        label.AddThemeStyleboxOverride("panel", label.box);
        label.AddChild(CreateLine(GetTextOf(item), GetColorOf(item.Rarity)));

        label.MouseEntered += () => label.ShowHovered(true);
        label.MouseExited  += () => label.ShowHovered(false);

        return label;
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            return;

        AcceptEvent();

        Clicked?.Invoke(this);
    }

    public void ShowHovered(bool isHovered)
    {
        box.BgColor = isHovered ? HoveredBoxColor : BoxColor;

        if (IsInstanceValid(Lootbag))
            Lootbag.SetHighlight(isHovered);
    }

    private static StyleBoxFlat CreateBox()
        => new()
        {
            BgColor             = BoxColor,
            ContentMarginLeft   = PaddingWidth,
            ContentMarginRight  = PaddingWidth,
            ContentMarginTop    = PaddingHeight,
            ContentMarginBottom = PaddingHeight
        };

    private static Label CreateLine(string text, Color color)
    {
        var line = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore };

        line.AddThemeFontSizeOverride("font_size", FontSize);
        line.AddThemeColorOverride("font_color", color);

        return line;
    }

    private static string GetTextOf(ItemInstance item)
    {
        var name = item.Rarity switch
        {
            ItemRarity.Magic                                        => item.AffixedName,
            ItemRarity.Rare when !string.IsNullOrEmpty(item.RareName) => $"{item.RareName} · {item.Definition.Name}",
            _                                                       => item.Definition.Name
        };

        return item.StackSize > 1 ? $"{name} ({item.StackSize})" : name;
    }

    //Dieselben Farben wie im Tooltip der Items
    private static Color GetColorOf(ItemRarity rarity)
        => rarity switch
        {
            ItemRarity.Magic => Colors.DodgerBlue,
            ItemRarity.Rare  => Colors.Yellow,
            _                => Colors.White
        };
}
