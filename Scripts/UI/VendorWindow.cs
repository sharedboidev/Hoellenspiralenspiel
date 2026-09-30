using System;
using Godot;
using Hoellenspiralenspiel.Interfaces;
using Hoellenspiralenspiel.Scripts.Controllers;
using Hoellenspiralenspiel.Scripts.Core.Economy;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.Objects;
using Hoellenspiralenspiel.Scripts.UI.Character;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.UI;

//Das Fenster des Händlers oben links: Verbrauchsgüter, gewürfelte Ausrüstung und der Rückkauf
public partial class VendorWindow
        : PanelContainer,
          ISideWindow
{
    private const float ReachSlackPx = 60f;

    private ItemGridView     buybackView;
    private CharacterItems   items;
    private VendorController keeper;
    private Label            levelLabel;
    private Label            messageLabel;
    private double           secMessageLeft;
    private CharacterSheet   sheet;
    private Fixture          source;
    private ItemGridView     stockView;
    private BaseUnit         user;
    private ItemGridView     waresView;

    [Export]
    public double MessageSec { get; set; } = 2.5;

    public bool IsOpen => Visible;

    private Trade Trade => keeper.Trade;

    public void Bind(IHero owner, CharacterSheet characterSheet)
    {
        items        = owner.Items;
        sheet        = characterSheet;
        waresView    = GetNode<ItemGridView>("%Consumables");
        stockView    = GetNode<ItemGridView>("%Equipment");
        buybackView  = GetNode<ItemGridView>("%Buyback");
        levelLabel   = GetNode<Label>("%LevelLabel");
        messageLabel = GetNode<Label>("%MessageLabel");

        GetNode<TabContainer>("%Tabs").GetTabBar().FocusMode = FocusModeEnum.None;

        Hide();
    }

    public void Connect(VendorController vendorController)
    {
        if (items is null || vendorController is null || keeper is not null)
            return;

        keeper = vendorController;

        BindView(waresView, keeper.Vendor.Wares, "Buy", OnWareClicked);
        BindView(stockView, keeper.Vendor.Stock, "Buy", OnStockClicked);
        BindView(buybackView, keeper.Vendor.Buyback, "Buy back", OnBuybackClicked);

        keeper.Vendor.Changed += Refresh;

        Refresh();
    }

    public override void _ExitTree()
    {
        if (keeper is not null)
            keeper.Vendor.Changed -= Refresh;
    }

    //Wer vom Händler wegläuft, braucht sein Fenster nicht mehr
    public override void _Process(double delta)
    {
        if (!Visible)
            return;

        if (!IsInstanceValid(source) || !IsInstanceValid(user) || !source.IsNear(user, ReachSlackPx))
        {
            Close();

            return;
        }

        if (secMessageLeft <= 0)
            return;

        secMessageLeft -= delta;

        if (secMessageLeft <= 0)
            messageLabel.Text = string.Empty;
    }

    //Ein Klick neben die Gitter verkauft das Item an der Maus ebenfalls
    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } && SellHeld())
            AcceptEvent();
    }

    public void ShowFor(Fixture merchant, BaseUnit customer)
    {
        if (keeper is null || merchant is null || customer is null)
            return;

        source = merchant;
        user   = customer;

        keeper.EnsureStocked();

        sheet.OpenFor(this);

        messageLabel.Text = string.Empty;

        Refresh();
        Show();
    }

    public void Close()
    {
        if (!Visible)
            return;

        Hide();

        source = null;

        items.ReturnHeld();
    }

    public bool OfferQuick(ItemInstance item)
    {
        Sell(item);

        return true;
    }

    public string GetPriceNote(ItemInstance item)
        => keeper is not null && Trade.CanSell(item) ? $"Sell: {Trade.GetSellPrice(item):N0} Gold" : "Cannot be sold";

    private void BindView(ItemGridView view, InventoryGrid grid, string verb, Action<InventoryItem, InputEventMouseButton> onClicked)
    {
        view.Bind(grid, items);

        view.PriceNote   =  item => $"{verb}: {Trade.GetBuyPrice(item):N0} Gold";
        view.ItemClicked += onClicked;
        view.SlotClicked += _ => SellHeld();
    }

    private void Refresh()
    {
        waresView.Refresh();
        stockView.Refresh();
        buybackView.Refresh();

        levelLabel.Text = $"Item level {keeper.Vendor.ItemLevel}";
    }

    //Mit Strg kauft ein Rechtsklick einen ganzen Stapel
    private void OnWareClicked(InventoryItem view, InputEventMouseButton click)
    {
        if (IsPurchase(click))
            Report(Trade.BuyWare(view.Item, click.CtrlPressed ? view.Item.Definition.MaxStackSize : 1));
    }

    private void OnStockClicked(InventoryItem view, InputEventMouseButton click)
    {
        if (IsPurchase(click))
            Report(Trade.BuyStock(view.Item));
    }

    private void OnBuybackClicked(InventoryItem view, InputEventMouseButton click)
    {
        if (IsPurchase(click))
            Report(Trade.BuyBack(view.Item));
    }

    //Der Rechtsklick kauft. Mit einem Item an der Maus ist ein Linksklick ins Fenster ein Verkauf
    private bool IsPurchase(InputEventMouseButton click)
    {
        if (click.ButtonIndex == MouseButton.Left)
            SellHeld();

        return click.ButtonIndex == MouseButton.Right && items.HeldItem is null;
    }

    private bool SellHeld()
    {
        if (items.HeldItem is null)
            return false;

        Sell(items.HeldItem);

        return true;
    }

    private void Sell(ItemInstance item)
        => Say(Trade.Sell(item) ? string.Empty : "The merchant does not want this.");

    private void Report(TradeResult result)
        => Say(result switch
        {
            TradeResult.NotEnoughGold => "Not enough gold.",
            TradeResult.NoRoom        => "Your inventory is full.",
            TradeResult.NotForSale    => "This is not for sale.",
            _                         => string.Empty
        });

    private void Say(string message)
    {
        messageLabel.Text = message;
        secMessageLeft    = MessageSec;
    }
}
