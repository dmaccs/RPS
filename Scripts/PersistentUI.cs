using Godot;
using System;

// This script should be attached to the root CanvasLayer of PersistentUI.tscn
public partial class PersistentUI : CanvasLayer
{
    private Label healthLabel;
    private Label goldLabel;
    private Label strengthLabel;
    private ItemList inventory;
    private ItemList relics;
    private PopupMenu itemContextMenu;

    // Map inventory index to item ID
    private System.Collections.Generic.List<string> itemIdMapping = new();

    public override void _Ready()
    {
        // Get references to UI elements under the CanvasLayer root
        healthLabel = GetNode<Label>("Stats/HealthLabel");
        goldLabel = GetNodeOrNull<Label>("Stats/GoldLabel");
        // The scene uses a label named "Strength Label" (with a space)
        strengthLabel = GetNode<Label>("Stats/Strength Label");
        inventory = GetNode<ItemList>("Inventory/ItemList");
        relics = GetNode<ItemList>("Relics/RelicList");

        // Create context menu for items
        itemContextMenu = new PopupMenu();
        itemContextMenu.Name = "ItemContextMenu";
        itemContextMenu.AddItem("Use", 0);
        itemContextMenu.AddItem("Discard", 1);
        itemContextMenu.IndexPressed += OnContextMenuItemSelected;
        AddChild(itemContextMenu);

        // Connect right-click on inventory
        inventory.ItemClicked += OnInventoryItemClicked;

        // Set mouse filter to ignore for non-interactive containers so input passes through.
        var statsControl = GetNode<Control>("Stats");
        statsControl.SetMouseFilter(Control.MouseFilterEnum.Ignore);
        healthLabel.SetMouseFilter(Control.MouseFilterEnum.Ignore);
        strengthLabel.SetMouseFilter(Control.MouseFilterEnum.Ignore);

        // Don't ignore inventory/relics - we need to interact with ItemLists

        UpdateUI();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationPredelete)
        {
            CleanupNodes();
        }
    }

    public override void _ExitTree()
    {
        CleanupNodes();
        base._ExitTree();
    }

    private bool cleanupDone = false;

    private void CleanupNodes()
    {
        if (cleanupDone) return; // Prevent double cleanup
        cleanupDone = true;

        // Disconnect signals to prevent leaks
        if (inventory != null && IsInstanceValid(inventory))
        {
            inventory.ItemClicked -= OnInventoryItemClicked;
            inventory = null;
        }

        if (itemContextMenu != null && IsInstanceValid(itemContextMenu))
        {
            itemContextMenu.IndexPressed -= OnContextMenuItemSelected;
            // Just QueueFree - don't RemoveChild first as that breaks child cleanup
            itemContextMenu.QueueFree();
            itemContextMenu = null;
        }
    }

    public void UpdateUI()
    {
        var gameState = GameState.Instance;
        if (gameState == null) return;

        // Update labels
        if (healthLabel != null)
            healthLabel.Text = $"Health: {gameState.PlayerHealth}/{gameState.MaxPlayerHealth}";

        if (goldLabel != null)
            goldLabel.Text = $"Gold: {gameState.PlayerGold}";

        // Update move level display (rock/paper/scissors)
        UpdateMoveLevels();

        // Update inventory and relics
        UpdateInventoryLists();

        GD.Print($"PersistentUI.UpdateUI: health={gameState.PlayerHealth}/{gameState.MaxPlayerHealth}, gold={gameState.PlayerGold}");
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

    private void UpdateInventoryLists()
    {
        var gameState = GameState.Instance;
        if (gameState == null) return;

        // Update consumable items list
        if (inventory != null)
        {
            inventory.Clear();
            itemIdMapping.Clear();
            var items = gameState.GetItems();

            // Count each item type
            var itemCounts = new System.Collections.Generic.Dictionary<string, int>();
            foreach (var itemId in items)
            {
                if (itemCounts.ContainsKey(itemId))
                    itemCounts[itemId]++;
                else
                    itemCounts[itemId] = 1;
            }

            // Display items with counts and build mapping
            foreach (var kvp in itemCounts)
            {
                var itemData = ItemDatabase.Instance.GetConsumable(kvp.Key);
                if (itemData != null)
                {
                    string displayText = kvp.Value > 1 ? $"{itemData.Name} x{kvp.Value}" : itemData.Name;
                    inventory.AddItem(displayText);
                    itemIdMapping.Add(kvp.Key); // Store item ID for this index
                }
            }
        }

        // Update relics list
        if (relics != null)
        {
            relics.Clear();
            var relicList = gameState.GetRelics();

            foreach (var relicId in relicList)
            {
                var relicData = ItemDatabase.Instance.GetRelic(relicId);
                if (relicData != null)
                {
                    relics.AddItem(relicData.Name);
                }
            }
        }
    }

    private void OnInventoryItemClicked(long index, Vector2 atPosition, long mouseButtonIndex)
    {
        // Right-click is button index 2
        if (mouseButtonIndex == (long)MouseButton.Right)
        {
            // Show context menu at mouse position
            itemContextMenu.Position = (Vector2I)GetViewport().GetMousePosition();
            itemContextMenu.Popup();

            // Store the selected index in metadata for later use
            itemContextMenu.SetMeta("selected_index", index);
        }
    }

    private void OnContextMenuItemSelected(long id)
    {
        // Get the selected item index from metadata
        if (!itemContextMenu.HasMeta("selected_index"))
            return;

        int index = (int)itemContextMenu.GetMeta("selected_index");

        if (index < 0 || index >= itemIdMapping.Count)
            return;

        string itemId = itemIdMapping[index];

        if (id == 0) // Use
        {
            UseItem(itemId);
        }
        else if (id == 1) // Discard
        {
            DiscardItem(itemId);
        }
    }

    private void UseItem(string itemId)
    {
        var itemData = ItemDatabase.Instance.GetConsumable(itemId);
        if (itemData == null || itemData.Effect == null)
        {
            GD.PrintErr($"Failed to use item: {itemId}");
            return;
        }

        // Check if we're in a battle scene
        var currentScene = GetTree().CurrentScene;
        var battleScene = currentScene as BattleScene;

        if (battleScene == null)
        {
            GD.Print("Items can only be used during battle!");
            return;
        }

        // Get player and enemy from battle scene
        var player = GameManager.Instance.Player;
        var enemy = battleScene.GetCurrentEnemy();

        if (player == null || enemy == null)
        {
            GD.PrintErr("Cannot use item: player or enemy not found");
            return;
        }

        // Apply item effect
        var effect = ItemEffectFactory.Create(itemData.Effect.EffectType);
        if (effect != null)
        {
            effect.Apply(player, enemy, itemData.Effect);

            // Remove item from inventory
            GameState.Instance.RemoveItem(itemId);

            GD.Print($"Used item: {itemData.Name}");
        }
        else
        {
            GD.PrintErr($"Unknown effect type: {itemData.Effect.EffectType}");
        }
    }

    private void DiscardItem(string itemId)
    {
        var itemData = ItemDatabase.Instance.GetConsumable(itemId);
        if (itemData != null)
        {
            GameState.Instance.RemoveItem(itemId);
            GD.Print($"Discarded item: {itemData.Name}");
        }
    }
}