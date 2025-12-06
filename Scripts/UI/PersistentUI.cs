using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Rps;

// This script should be attached to the root CanvasLayer of PersistentUI.tscn
public partial class PersistentUI : CanvasLayer
{
    private Label healthLabel;
    private Label goldLabel;
    private Label strengthLabel;
    private ItemList relics;
    private PopupMenu itemContextMenu;

    // Inventory slots (3 fixed slots)
    private Panel[] itemSlots = new Panel[3];
    private Label[] itemSlotLabels = new Label[3];
    private string[] itemSlotIds = new string[3]; // Track which item is in each slot

    // Tooltip elements
    private Panel tooltip;
    private Label tooltipLabel;
    private bool isTooltipActive = false;

    // Throw inventory button
    private Button throwInventoryButton;
    private Node currentInventoryInstance = null;

    public override void _Ready()
    {
        // Get references to UI elements under the CanvasLayer root
        healthLabel = GetNode<Label>("Stats/HealthLabel");
        goldLabel = GetNodeOrNull<Label>("Stats/GoldLabel");
        // The scene uses a label named "Strength Label" (with a space)
        strengthLabel = GetNode<Label>("Stats/Strength Label");
        relics = GetNode<ItemList>("Relics/RelicList");

        // Get inventory slot references
        for (int i = 0; i < 3; i++)
        {
            itemSlots[i] = GetNode<Panel>($"Inventory/ItemSlots/Slot{i + 1}");
            itemSlotLabels[i] = GetNode<Label>($"Inventory/ItemSlots/Slot{i + 1}/Label");
            itemSlotIds[i] = null;

            // Connect mouse events for each slot
            int slotIndex = i; // Capture for lambda
            itemSlots[i].GuiInput += (inputEvent) => OnSlotInput(slotIndex, inputEvent);
            itemSlots[i].MouseEntered += () => { isTooltipActive = true; };
            itemSlots[i].MouseExited += () => { isTooltipActive = false; tooltip.Visible = false; };
        }

        // Get tooltip references
        tooltip = GetNode<Panel>("Tooltip");
        tooltipLabel = GetNode<Label>("Tooltip/TooltipLabel");

        // Create context menu for items
        itemContextMenu = new PopupMenu();
        itemContextMenu.Name = "ItemContextMenu";
        itemContextMenu.AddItem("Use", 0);
        itemContextMenu.AddItem("Discard", 1);
        itemContextMenu.IndexPressed += OnContextMenuItemSelected;
        AddChild(itemContextMenu);

        // Connect mouse events for tooltips on relics
        relics.MouseEntered += () => isTooltipActive = true;
        relics.MouseExited += () => { isTooltipActive = false; tooltip.Visible = false; };

        // Set mouse filter to ignore for non-interactive containers so input passes through.
        var statsControl = GetNode<Control>("Stats");
        statsControl.SetMouseFilter(Control.MouseFilterEnum.Ignore);
        healthLabel.SetMouseFilter(Control.MouseFilterEnum.Ignore);
        strengthLabel.SetMouseFilter(Control.MouseFilterEnum.Ignore);

        // Get or create throw inventory button
        throwInventoryButton = GetNodeOrNull<Button>("Stats/ThrowInventoryButton");
        if (throwInventoryButton != null)
        {
            throwInventoryButton.Pressed += OnThrowInventoryButtonPressed;
        }

        UpdateUI();
    }

    private void OnThrowInventoryButtonPressed()
    {
        // Don't open if one is already open
        if (currentInventoryInstance != null && IsInstanceValid(currentInventoryInstance))
        {
            return;
        }

        // Load and show inventory scene as overlay
        var inventoryScene = GD.Load<PackedScene>("res://Scenes/Inventory/InventoryScene.tscn");
        if (inventoryScene != null)
        {
            currentInventoryInstance = inventoryScene.Instantiate();
            GetTree().CurrentScene.AddChild(currentInventoryInstance);
        }
        else
        {
            GD.PrintErr("Failed to load InventoryScene.tscn");
        }
    }

    private void OnSlotInput(int slotIndex, InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton mouseButton && mouseButton.Pressed)
        {
            // Right-click to show context menu
            if (mouseButton.ButtonIndex == MouseButton.Right && itemSlotIds[slotIndex] != null)
            {
                itemContextMenu.Position = (Vector2I)GetViewport().GetMousePosition();
                itemContextMenu.Popup();
                itemContextMenu.SetMeta("selected_slot", slotIndex);
            }
        }
    }

    public override void _Process(double delta)
    {
        if (!isTooltipActive)
            return;

        // Get mouse position relative to the viewport
        var mousePos = GetViewport().GetMousePosition();

        // Check if hovering over inventory slots
        for (int i = 0; i < 3; i++)
        {
            var slotRect = itemSlots[i].GetGlobalRect();
            if (slotRect.HasPoint(mousePos) && itemSlotIds[i] != null)
            {
                var itemData = ItemDatabase.Instance.GetConsumable(itemSlotIds[i]);
                if (itemData != null)
                {
                    ShowTooltip(itemData.Description, mousePos);
                    return;
                }
            }
        }

        // Check if hovering over relics
        var relicsRect = relics.GetGlobalRect();
        if (relicsRect.HasPoint(mousePos))
        {
            var localPos = relics.GetLocalMousePosition();
            var relicIndex = relics.GetItemAtPosition(localPos, true);

            if (relicIndex >= 0)
            {
                var relicList = GameState.Instance.GetRelics();
                if (relicIndex < relicList.Count)
                {
                    var relicId = relicList[relicIndex];
                    var relicData = ItemDatabase.Instance.GetRelic(relicId);
                    if (relicData != null)
                    {
                        ShowTooltip(relicData.Description, mousePos);
                        return;
                    }
                }
            }
        }

        // If not hovering over any item, hide tooltip
        tooltip.Visible = false;
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

        // Disconnect slot signals
        // Note: GuiInput lambda connections will be cleaned up with the object

        if (relics != null && IsInstanceValid(relics))
        {
            // Lambda connections will be cleaned up with the object
            relics = null;
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
        UpdateInventorySlots();

        GD.Print($"PersistentUI.UpdateUI: health={gameState.PlayerHealth}/{gameState.MaxPlayerHealth}, gold={gameState.PlayerGold}");
    }

    // Update move-level display - now shows equipped throws from new system
    private void UpdateMoveLevels()
    {
        if (strengthLabel == null) return;
        var player = GameManager.Instance?.Player;
        if (player == null)
            return;

        var lines = new List<string>();
        lines.Add("Equipped:");

        foreach (var throwData in player.EquippedThrows)
        {
            if (throwData == null) continue;

            // Format: "Rock (1 dmg) [R]"
            string attrStr = string.Join("/", throwData.Attributes.Select(a => a.ToString()[0]));
            lines.Add($"  {throwData.Name} ({throwData.Effect.BaseDamage}dmg) [{attrStr}]");
        }

        // Show synergy counts
        int rockCount = player.GetEquippedAttributeCount(ThrowAttribute.Rock);
        int paperCount = player.GetEquippedAttributeCount(ThrowAttribute.Paper);
        int scissorsCount = player.GetEquippedAttributeCount(ThrowAttribute.Scissors);

        lines.Add("---");
        lines.Add($"R:{rockCount} P:{paperCount} S:{scissorsCount}");

        strengthLabel.Text = string.Join("\n", lines);
    }

    private void UpdateInventorySlots()
    {
        var gameState = GameState.Instance;
        if (gameState == null) return;

        var items = gameState.GetItems();

        // Count each item type
        var itemCounts = new Dictionary<string, int>();
        foreach (var itemId in items)
        {
            if (itemCounts.ContainsKey(itemId))
                itemCounts[itemId]++;
            else
                itemCounts[itemId] = 1;
        }

        // Clear all slots first
        for (int i = 0; i < 3; i++)
        {
            itemSlotIds[i] = null;
            itemSlotLabels[i].Text = "Empty";
        }

        // Fill slots with items (up to 3)
        int slotIndex = 0;
        foreach (var kvp in itemCounts)
        {
            if (slotIndex >= 3) break; // Only 3 slots available

            var itemData = ItemDatabase.Instance.GetConsumable(kvp.Key);
            if (itemData != null)
            {
                itemSlotIds[slotIndex] = kvp.Key;
                string displayText = kvp.Value > 1 ? $"{itemData.Name}\nx{kvp.Value}" : itemData.Name;
                itemSlotLabels[slotIndex].Text = displayText;
                slotIndex++;
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

    private void OnContextMenuItemSelected(long id)
    {
        // Get the selected slot index from metadata
        if (!itemContextMenu.HasMeta("selected_slot"))
            return;

        int slotIndex = (int)itemContextMenu.GetMeta("selected_slot");

        if (slotIndex < 0 || slotIndex >= 3 || itemSlotIds[slotIndex] == null)
            return;

        string itemId = itemSlotIds[slotIndex];

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

    private void ShowTooltip(string description, Vector2 mousePos)
    {
        if (string.IsNullOrEmpty(description))
        {
            tooltip.Visible = false;
            return;
        }

        // Update tooltip text
        tooltipLabel.Text = description;

        // Calculate tooltip size based on text
        // Set a reasonable max width
        const int maxWidth = 250;
        //const int padding = 10;

        // Position tooltip offset from mouse
        var offsetX = 15;
        var offsetY = 15;

        tooltip.Position = new Vector2(mousePos.X + offsetX, mousePos.Y + offsetY);
        tooltip.Size = new Vector2(maxWidth, 0); // Height will auto-adjust

        // Make sure tooltip doesn't go off-screen
        var viewportSize = GetViewport().GetVisibleRect().Size;
        if (tooltip.Position.X + maxWidth > viewportSize.X)
        {
            tooltip.Position = new Vector2(mousePos.X - maxWidth - offsetX, tooltip.Position.Y);
        }
        if (tooltip.Position.Y + 100 > viewportSize.Y) // Approximate height
        {
            tooltip.Position = new Vector2(tooltip.Position.X, mousePos.Y - 100 - offsetY);
        }

        tooltip.Visible = true;
    }
}
