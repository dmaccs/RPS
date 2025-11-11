using Godot;
using System;

// This script should be attached to the root CanvasLayer of PersistentUI.tscn
public partial class PersistentUI : CanvasLayer
{
    private Label healthLabel;
    private Label strengthLabel;
    private ItemList inventory;
    private ItemList relics;

    public override void _Ready()
    {
        // Get references to UI elements under the CanvasLayer root
        healthLabel = GetNode<Label>("Stats/HealthLabel");
        // The scene uses a label named "Strength Label" (with a space)
        strengthLabel = GetNode<Label>("Stats/Strength Label");
        inventory = GetNode<ItemList>("Inventory/ItemList");
        relics = GetNode<ItemList>("Relics/RelicList");

        // Set mouse filter to ignore for non-interactive containers so input passes through.
        var statsControl = GetNode<Control>("Stats");
        statsControl.SetMouseFilter(Control.MouseFilterEnum.Ignore);
        healthLabel.SetMouseFilter(Control.MouseFilterEnum.Ignore);
        strengthLabel.SetMouseFilter(Control.MouseFilterEnum.Ignore);

        var inventoryControl = GetNode<Control>("Inventory");
        inventoryControl.SetMouseFilter(Control.MouseFilterEnum.Ignore);

        var relicsControl = GetNode<Control>("Relics");
        relicsControl.SetMouseFilter(Control.MouseFilterEnum.Ignore);

        UpdateUI();
    }

    public void UpdateUI()
    {
        var gameState = GameState.Instance;
        if (gameState == null) return;

        // Update labels
        if (healthLabel != null)
            healthLabel.Text = $"Health: {gameState.PlayerHealth}/{gameState.MaxPlayerHealth}";

        // Update move level display (rock/paper/scissors)
        UpdateMoveLevels();

        // Inventory/relic lists can be updated here when needed
        GD.Print($"PersistentUI.UpdateUI: health={gameState.PlayerHealth}/{gameState.MaxPlayerHealth}");
    }

    // Update move-level display (rock/paper/scissors)
    private void UpdateMoveLevels()
    {
        if (strengthLabel == null) return;
        var player = GameManager.Instance?.Player;
        if (player == null)
            return;

        int rockLevel = 0;
        int paperLevel = 0;
        int scissorsLevel = 0;

        foreach (var move in player.CurrentThrows)
        {
            switch (move.Type)
            {
                case Rps.Throws.rock:
                    rockLevel = move.Level; break;
                case Rps.Throws.paper:
                    paperLevel = move.Level; break;
                case Rps.Throws.scissors:
                    scissorsLevel = move.Level; break;
            }
        }

        strengthLabel.Text = $"Rock: {rockLevel}\nPaper: {paperLevel}\nScissors: {scissorsLevel}";
    }
}