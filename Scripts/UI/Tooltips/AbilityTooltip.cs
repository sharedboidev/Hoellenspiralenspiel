using System;
using Godot;
using Hoellenspiralenspiel.Interfaces;
using Environment = System.Environment;

namespace Hoellenspiralenspiel.Scripts.UI.Tooltips;

public partial class AbilityTooltip : BaseTooltip
{
    private double timeSinceLastToggle;
    private bool   tooltipIsShowing;
    private double visibilityCooldownMs = 0.5;

    public override void _Process(double delta) => ProcessTestDisplay(delta);

    private void ProcessTestDisplay(double delta)
    {
        if (Input.IsKeyPressed(Key.T) && timeSinceLastToggle >= visibilityCooldownMs)
        {
            timeSinceLastToggle = 0;

            if (tooltipIsShowing)
            {
                Hide();
                tooltipIsShowing = false;
            }
            else
            {
                var tooltipTestObject = new TestTooltipObjectContainer
                {
                    Position      = GetViewport().GetMousePosition(),
                    ContainedItem = new TestTooltipObject(),
                    Size          = new Vector2(10, 10)
                };

                Show(tooltipTestObject);
                tooltipIsShowing = true;
            }
        }

        timeSinceLastToggle += delta;
    }
}

public class TestTooltipObject : ITooltipObject
{
    public string TooltipTitle { get; set; } = "Test Titel!";

    public string GetTooltipDescription() => $"I Bims eine neue Tooltipbeschreibung{Environment.NewLine}Wir haben den {DateTime.Now:dd.MM.yyyy}";
}

public class TestTooltipObjectContainer : ITooltipObjectContainer
{
    public ITooltipObject ContainedItem { get; set; }
    public Vector2        Position      { get; set; }
    public Vector2        Size          { get; set; }
}