using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Rps;

public partial class Rewards : Control
{
    private Button button1;
    private Button button2;
    private Button skipButton;
    private Control throwSelectionPanel;
    private Control slotSelectionPanel;
    private Control confirmationPanel;

    // Throw selection buttons
    private Button[] throwSelectionButtons = new Button[3];
    private Dictionary<Button, Action> buttonHandlers = new Dictionary<Button, Action>();

    // Slot selection buttons (3 equipped + 3 inventory)
    private Button[] slotButtons = new Button[6];

    // Confirmation dialog elements
    private Label confirmationLabel;
    private Button confirmYesButton;
    private Button confirmNoButton;

    // Offered throws for selection
    private List<ThrowData> offeredThrows = new List<ThrowData>();

    // Currently selected throw waiting for slot placement
    private ThrowData pendingThrow = null;

    // Pending slot for confirmation
    private int pendingSlotIndex = -1;
    private bool pendingSlotIsEquipped = false;
    private ThrowData throwToReplace = null;

    // Track if throw reward has been claimed
    private bool throwRewardClaimed = false;

    private Label goldLabel;

    // Tooltip elements
    private Panel tooltipPanel;
    private Label tooltipLabel;

    // Random reward data
    private RewardType randomRewardType;
    private object randomRewardData;
    private bool randomRewardClaimed = false;

    private enum RewardType
    {
        Item,
        Gold,
        Health,
        Relic,
        MaxHealth
    }

    public override void _Ready()
    {
        goldLabel = GetNode<Label>("VBoxContainer/BoxContainer/Label2");
        button1 = GetNode<Button>("VBoxContainer/BoxContainer/Button");
        button2 = GetNode<Button>("VBoxContainer/BoxContainer/Button2");
        skipButton = GetNode<Button>("VBoxContainer/Button3");

        // Get throw selection panel and buttons
        throwSelectionPanel = GetNode<Control>("ThrowSelectionPanel");
        throwSelectionButtons[0] = GetNode<Button>("ThrowSelectionPanel/VBoxContainer/ThrowButton1");
        throwSelectionButtons[1] = GetNode<Button>("ThrowSelectionPanel/VBoxContainer/ThrowButton2");
        throwSelectionButtons[2] = GetNode<Button>("ThrowSelectionPanel/VBoxContainer/ThrowButton3");

        // Hide throw selection panel initially
        throwSelectionPanel.Visible = false;

        // Create slot selection panel
        CreateSlotSelectionPanel();

        // Create confirmation panel
        CreateConfirmationPanel();

        // Create tooltip panel
        CreateTooltipPanel();

        // Award random base gold (5-15)
        int baseGold = RngManager.Instance.Rng.RandiRange(5, 15);
        GameState.Instance.AddGold(baseGold);
        goldLabel.Text = $"You received {baseGold} Gold!";

        // Generate random reward
        GenerateRandomReward();

        // Set button texts
        button1.Text = "Get a new throw";
        button2.Text = GetRandomRewardDescription();
        skipButton.Text = "Continue";

        // Update button states based on inventory
        UpdateRewardButtonStates();

        button1.Pressed += OnNewThrowChosen;
        button2.Pressed += OnRandomRewardChosen;
        skipButton.Pressed += OnSkipChosen;
    }

    private void CreateTooltipPanel()
    {
        tooltipPanel = new Panel();
        tooltipPanel.Visible = false;
        tooltipPanel.MouseFilter = Control.MouseFilterEnum.Ignore;
        tooltipPanel.ZIndex = 100;
        AddChild(tooltipPanel);

        tooltipLabel = new Label();
        tooltipLabel.AutowrapMode = TextServer.AutowrapMode.Word;
        tooltipLabel.CustomMinimumSize = new Vector2(250, 0);
        tooltipPanel.AddChild(tooltipLabel);

        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        styleBox.BorderColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        styleBox.SetBorderWidthAll(2);
        styleBox.SetContentMarginAll(8);
        tooltipPanel.AddThemeStyleboxOverride("panel", styleBox);
    }

    private void CreateSlotSelectionPanel()
    {
        slotSelectionPanel = new Control();
        slotSelectionPanel.Visible = false;
        slotSelectionPanel.SetAnchorsPreset(Control.LayoutPreset.Center);
        AddChild(slotSelectionPanel);

        var vbox = new VBoxContainer();
        vbox.Position = new Vector2(-150, -100);
        slotSelectionPanel.AddChild(vbox);

        var titleLabel = new Label();
        titleLabel.Text = "Choose a slot:";
        vbox.AddChild(titleLabel);

        // Equipped slots section
        var equippedLabel = new Label();
        equippedLabel.Text = "-- Equipped --";
        vbox.AddChild(equippedLabel);

        var equippedHbox = new HBoxContainer();
        vbox.AddChild(equippedHbox);

        for (int i = 0; i < 3; i++)
        {
            var button = new Button();
            button.CustomMinimumSize = new Vector2(100, 40);
            equippedHbox.AddChild(button);
            slotButtons[i] = button;

            int slotIndex = i;
            button.Pressed += () => OnSlotSelected(slotIndex, true);
        }

        // Inventory slots section
        var inventoryLabel = new Label();
        inventoryLabel.Text = "-- Inventory --";
        vbox.AddChild(inventoryLabel);

        var inventoryHbox = new HBoxContainer();
        vbox.AddChild(inventoryHbox);

        for (int i = 0; i < 3; i++)
        {
            var button = new Button();
            button.CustomMinimumSize = new Vector2(100, 40);
            inventoryHbox.AddChild(button);
            slotButtons[i + 3] = button;

            int slotIndex = i;
            button.Pressed += () => OnSlotSelected(slotIndex, false);
        }

        // Cancel button
        var cancelButton = new Button();
        cancelButton.Text = "Cancel";
        cancelButton.Pressed += OnSlotSelectionCancelled;
        vbox.AddChild(cancelButton);
    }

    private void CreateConfirmationPanel()
    {
        confirmationPanel = new Control();
        confirmationPanel.Visible = false;
        confirmationPanel.SetAnchorsPreset(Control.LayoutPreset.Center);
        AddChild(confirmationPanel);

        var panel = new Panel();
        panel.Position = new Vector2(-150, -50);
        panel.CustomMinimumSize = new Vector2(300, 100);
        confirmationPanel.AddChild(panel);

        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = new Color(0.15f, 0.15f, 0.15f, 0.98f);
        styleBox.BorderColor = new Color(0.6f, 0.2f, 0.2f, 1f);
        styleBox.SetBorderWidthAll(2);
        styleBox.SetContentMarginAll(10);
        panel.AddThemeStyleboxOverride("panel", styleBox);

        var vbox = new VBoxContainer();
        vbox.Position = new Vector2(10, 10);
        panel.AddChild(vbox);

        confirmationLabel = new Label();
        confirmationLabel.Text = "Are you sure?";
        confirmationLabel.AutowrapMode = TextServer.AutowrapMode.Word;
        confirmationLabel.CustomMinimumSize = new Vector2(280, 0);
        vbox.AddChild(confirmationLabel);

        var buttonHbox = new HBoxContainer();
        vbox.AddChild(buttonHbox);

        confirmYesButton = new Button();
        confirmYesButton.Text = "Yes, Replace";
        confirmYesButton.Pressed += OnConfirmReplace;
        buttonHbox.AddChild(confirmYesButton);

        confirmNoButton = new Button();
        confirmNoButton.Text = "No, Cancel";
        confirmNoButton.Pressed += OnCancelReplace;
        buttonHbox.AddChild(confirmNoButton);
    }

    private void UpdateSlotSelectionButtons()
    {
        var player = GameManager.Instance.Player;
        bool hasEmptySlot = HasAnyEmptySlot();

        // Update equipped slot buttons
        for (int i = 0; i < 3; i++)
        {
            var throwData = player.EquippedThrows[i];
            if (throwData != null)
            {
                slotButtons[i].Text = throwData.Name;
                // Disable occupied slots if there are empty slots available
                slotButtons[i].Disabled = hasEmptySlot;
            }
            else
            {
                slotButtons[i].Text = "(Empty)";
                slotButtons[i].Disabled = false;
            }
        }

        // Update inventory slot buttons
        for (int i = 0; i < 3; i++)
        {
            var throwData = player.InventoryThrows[i];
            if (throwData != null)
            {
                slotButtons[i + 3].Text = throwData.Name;
                // Disable occupied slots if there are empty slots available
                slotButtons[i + 3].Disabled = hasEmptySlot;
            }
            else
            {
                slotButtons[i + 3].Text = "(Empty)";
                slotButtons[i + 3].Disabled = false;
            }
        }
    }

    private bool HasAnyEmptySlot()
    {
        var player = GameManager.Instance.Player;

        for (int i = 0; i < 3; i++)
        {
            if (player.EquippedThrows[i] == null) return true;
            if (player.InventoryThrows[i] == null) return true;
        }

        return false;
    }

    public override void _Process(double delta)
    {
        // Update button states each frame to react to inventory changes
        UpdateRewardButtonStates();

        // Handle tooltip display
        UpdateTooltip();
    }

    private void UpdateTooltip()
    {
        Vector2 mousePos = GetGlobalMousePosition();
        bool hovering = false;

        // Check throw selection buttons
        if (throwSelectionPanel.Visible)
        {
            for (int i = 0; i < throwSelectionButtons.Length && i < offeredThrows.Count; i++)
            {
                var button = throwSelectionButtons[i];
                if (button != null && button.Visible && IsInstanceValid(button))
                {
                    Rect2 buttonRect = button.GetGlobalRect();
                    if (buttonRect.HasPoint(mousePos))
                    {
                        ShowTooltip(offeredThrows[i], mousePos);
                        hovering = true;
                        break;
                    }
                }
            }
        }

        // Check slot selection buttons for existing throws
        if (slotSelectionPanel.Visible && !hovering)
        {
            var player = GameManager.Instance.Player;
            for (int i = 0; i < 6; i++)
            {
                var button = slotButtons[i];
                if (button != null && IsInstanceValid(button))
                {
                    Rect2 buttonRect = button.GetGlobalRect();
                    if (buttonRect.HasPoint(mousePos))
                    {
                        ThrowData slotThrow = i < 3 ? player.EquippedThrows[i] : player.InventoryThrows[i - 3];
                        if (slotThrow != null)
                        {
                            ShowTooltip(slotThrow, mousePos);
                            hovering = true;
                        }
                        break;
                    }
                }
            }
        }

        if (!hovering)
        {
            HideTooltip();
        }
    }

    private void ShowTooltip(ThrowData throwData, Vector2 mousePos)
    {
        if (throwData == null)
            return;

        string text = $"{throwData.Name} ({throwData.Rarity})\n{throwData.Description}";
        tooltipLabel.Text = text;
        tooltipPanel.Visible = true;

        tooltipPanel.Position = mousePos + new Vector2(15, 15);

        // Keep tooltip on screen
        Vector2 viewportSize = GetViewportRect().Size;
        Vector2 tooltipSize = tooltipPanel.Size;

        if (tooltipPanel.Position.X + tooltipSize.X > viewportSize.X)
        {
            tooltipPanel.Position = new Vector2(tooltipPanel.Position.X - tooltipSize.X - 30, tooltipPanel.Position.Y);
        }
        if (tooltipPanel.Position.Y + tooltipSize.Y > viewportSize.Y)
        {
            tooltipPanel.Position = new Vector2(tooltipPanel.Position.X, tooltipPanel.Position.Y - tooltipSize.Y - 30);
        }
    }

    private void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.Visible = false;
        }
    }

    private void UpdateRewardButtonStates()
    {
        // Disable throw button if already claimed
        if (throwRewardClaimed)
        {
            button1.Disabled = true;
            button1.Text = "Throw claimed";
        }

        // Check if random reward is an item and inventory is full
        if (randomRewardClaimed)
        {
            button2.Disabled = true;
            button2.Text = "Reward claimed";
        }
        else if (randomRewardType == RewardType.Item)
        {
            bool inventoryFull = GameState.Instance.GetItems().Count >= 3;
            button2.Disabled = inventoryFull;

            // Update button text to show inventory status
            string baseText = GetRandomRewardDescription();
            button2.Text = inventoryFull ? baseText + " (Inventory Full)" : baseText;
        }
    }

    private void UpdateThrowButtons()
    {
        // Select 3 random throws weighted by rarity
        offeredThrows = ThrowDatabase.Instance.SelectRandomThrowsByRarity(3, RngManager.Instance.Rng);

        // Update each button
        for (int i = 0; i < throwSelectionButtons.Length; i++)
        {
            var button = throwSelectionButtons[i];

            // If we have an offered throw for this slot
            if (i < offeredThrows.Count)
            {
                var throwData = offeredThrows[i];
                button.Text = $"{throwData.Name} ({throwData.Rarity})";
                button.Visible = true;

                // Remove old handler if it exists
                if (buttonHandlers.TryGetValue(button, out var oldHandler))
                {
                    button.Pressed -= oldHandler;
                    buttonHandlers.Remove(button);
                }

                // Create and store new handler
                int index = i;
                Action newHandler = () => OnThrowSelected(index);
                buttonHandlers[button] = newHandler;
                button.Pressed += newHandler;
            }
            else
            {
                // No throw in this slot
                button.Visible = false;
            }
        }
    }

    private void GenerateRandomReward()
    {
        // Weighted random selection
        // Weights: Item(40), Gold(25), Health(20), Relic(10), MaxHealth(5)
        int roll = RngManager.Instance.Rng.RandiRange(1, 100);

        if (roll <= 40) // 40% - Item (Consumable)
        {
            randomRewardType = RewardType.Item;
            var commonItems = ItemDatabase.Instance.GetConsumablesByRarity(ItemRarity.Common);
            var uncommonItems = ItemDatabase.Instance.GetConsumablesByRarity(ItemRarity.Uncommon);

            // 70% common, 30% uncommon
            List<ConsumableItemData> itemPool;
            if (RngManager.Instance.Rng.RandiRange(1, 100) <= 70)
                itemPool = commonItems;
            else
                itemPool = uncommonItems;

            if (itemPool.Count > 0)
            {
                int index = RngManager.Instance.Rng.RandiRange(0, itemPool.Count - 1);
                randomRewardData = itemPool[index];
            }
        }
        else if (roll <= 65) // 25% - Extra Gold
        {
            randomRewardType = RewardType.Gold;
            randomRewardData = RngManager.Instance.Rng.RandiRange(15, 30);
        }
        else if (roll <= 85) // 20% - Health
        {
            randomRewardType = RewardType.Health;
            randomRewardData = RngManager.Instance.Rng.RandiRange(3, 7);
        }
        else if (roll <= 95) // 10% - Relic
        {
            randomRewardType = RewardType.Relic;
            var uncommonRelics = ItemDatabase.Instance.GetUnseenRelicsByRarity(ItemRarity.Uncommon);
            var rareRelics = ItemDatabase.Instance.GetUnseenRelicsByRarity(ItemRarity.Rare);

            // If all relics of a rarity have been seen, use all relics of that rarity
            if (uncommonRelics.Count == 0)
                uncommonRelics = ItemDatabase.Instance.GetRelicsByRarity(ItemRarity.Uncommon);
            if (rareRelics.Count == 0)
                rareRelics = ItemDatabase.Instance.GetRelicsByRarity(ItemRarity.Rare);

            // 60% uncommon, 40% rare
            List<RelicData> relicPool;
            if (RngManager.Instance.Rng.RandiRange(1, 100) <= 60)
                relicPool = uncommonRelics;
            else
                relicPool = rareRelics;

            if (relicPool.Count > 0)
            {
                int index = RngManager.Instance.Rng.RandiRange(0, relicPool.Count - 1);
                randomRewardData = relicPool[index];

                // Mark relic as seen when it appears as a reward option
                GameState.Instance.MarkRelicAsSeen(relicPool[index].Id);
            }
        }
        else // 5% - Max Health
        {
            randomRewardType = RewardType.MaxHealth;
            randomRewardData = RngManager.Instance.Rng.RandiRange(2, 3);
        }
    }

    private string GetRandomRewardDescription()
    {
        switch (randomRewardType)
        {
            case RewardType.Item:
                var item = randomRewardData as ConsumableItemData;
                return item != null ? $"Get {item.Name}" : "Get Item";

            case RewardType.Gold:
                return $"Get {randomRewardData} Gold";

            case RewardType.Health:
                return $"Restore {randomRewardData} HP";

            case RewardType.Relic:
                var relic = randomRewardData as RelicData;
                return relic != null ? $"Get {relic.Name}" : "Get Relic";

            case RewardType.MaxHealth:
                return $"Increase Max HP by {randomRewardData}";

            default:
                return "Get Random Reward";
        }
    }

    private void ApplyRandomReward()
    {
        switch (randomRewardType)
        {
            case RewardType.Item:
                var item = randomRewardData as ConsumableItemData;
                if (item != null)
                {
                    // Safety check - shouldn't happen since button is disabled when full
                    if (!GameState.Instance.AddItem(item.Id))
                    {
                        GD.PrintErr("Failed to add item - inventory full!");
                        return;
                    }
                    GD.Print($"Player received item: {item.Name}");
                }
                break;

            case RewardType.Gold:
                int goldAmount = (int)randomRewardData;
                GameState.Instance.AddGold(goldAmount);
                GD.Print($"Player received {goldAmount} extra gold");
                break;

            case RewardType.Health:
                int healthAmount = (int)randomRewardData;
                GameState.Instance.ModifyHealth(healthAmount);
                GD.Print($"Player restored {healthAmount} HP");
                break;

            case RewardType.Relic:
                var relic = randomRewardData as RelicData;
                if (relic != null)
                {
                    GameState.Instance.AddRelic(relic.Id);
                    // Apply relic effects immediately
                    ApplyRelicEffects(relic);
                    GD.Print($"Player received relic: {relic.Name}");
                }
                break;

            case RewardType.MaxHealth:
                int maxHpIncrease = (int)randomRewardData;
                GameState.Instance.IncreaseMaxHealth(maxHpIncrease);
                GD.Print($"Player max HP increased by {maxHpIncrease}");
                break;
        }

        randomRewardClaimed = true;
    }

    private void ApplyRelicEffects(RelicData relic)
    {
        // Apply relic effect using the ItemEffectFactory
        if (relic?.Effect != null)
        {
            var effect = ItemEffectFactory.Create(relic.Effect.EffectType);
            effect?.Apply(GameManager.Instance.Player, null, relic.Effect);
        }
    }

    private void OnNewThrowChosen()
    {
        // Hide main buttons and show throw selection
        button1.Visible = false;
        button2.Visible = false;
        skipButton.Visible = false;

        // Update the throw buttons with random offers
        UpdateThrowButtons();

        throwSelectionPanel.Visible = true;

        GD.Print("Player chose to get a new throw");
    }

    private void OnRandomRewardChosen()
    {
        ApplyRandomReward();
        // Don't continue to next scene, stay on rewards screen
    }

    private void OnThrowSelected(int index)
    {
        if (index < 0 || index >= offeredThrows.Count)
            return;

        // Store the selected throw and show slot selection
        pendingThrow = offeredThrows[index];

        // Hide throw selection and show slot selection
        throwSelectionPanel.Visible = false;
        UpdateSlotSelectionButtons();
        slotSelectionPanel.Visible = true;

        GD.Print($"Player selected throw: {pendingThrow.Name}, choosing slot...");
    }

    private void OnSlotSelected(int slotIndex, bool isEquipped)
    {
        if (pendingThrow == null)
            return;

        var player = GameManager.Instance.Player;

        // Check if the slot is occupied
        ThrowData existingThrow = isEquipped
            ? player.EquippedThrows[slotIndex]
            : player.InventoryThrows[slotIndex];

        if (existingThrow != null)
        {
            // Slot is occupied - this should only happen when all slots are full
            // Show confirmation dialog
            throwToReplace = existingThrow;
            pendingSlotIndex = slotIndex;
            pendingSlotIsEquipped = isEquipped;

            confirmationLabel.Text = $"Are you sure you want to remove \"{existingThrow.Name}\"?";
            slotSelectionPanel.Visible = false;
            confirmationPanel.Visible = true;
        }
        else
        {
            // Slot is empty, place directly
            PlaceThrowInSlot(slotIndex, isEquipped);
        }
    }

    private void OnConfirmReplace()
    {
        confirmationPanel.Visible = false;
        PlaceThrowInSlot(pendingSlotIndex, pendingSlotIsEquipped);
        throwToReplace = null;
    }

    private void OnCancelReplace()
    {
        confirmationPanel.Visible = false;
        throwToReplace = null;
        pendingSlotIndex = -1;

        // Go back to slot selection
        UpdateSlotSelectionButtons();
        slotSelectionPanel.Visible = true;
    }

    private void PlaceThrowInSlot(int slotIndex, bool isEquipped)
    {
        var player = GameManager.Instance.Player;
        var throwInstance = ThrowDatabase.Instance.CreateInstance(pendingThrow.Id);

        if (isEquipped)
        {
            player.SetEquippedSlot(slotIndex, throwInstance);
            GD.Print($"Placed {pendingThrow.Name} in equipped slot {slotIndex}");
        }
        else
        {
            player.SetInventorySlot(slotIndex, throwInstance);
            GD.Print($"Placed {pendingThrow.Name} in inventory slot {slotIndex}");
        }

        pendingThrow = null;
        throwRewardClaimed = true;
        GameState.Instance.RefreshUI();

        // Return to main reward buttons instead of continuing
        ReturnToMainButtons();
    }

    private void ReturnToMainButtons()
    {
        slotSelectionPanel.Visible = false;
        throwSelectionPanel.Visible = false;
        confirmationPanel.Visible = false;

        button1.Visible = true;
        button2.Visible = true;
        skipButton.Visible = true;

        // Update button states (will disable claimed rewards)
        UpdateRewardButtonStates();
    }

    private void OnSlotSelectionCancelled()
    {
        // Go back to throw selection
        pendingThrow = null;
        slotSelectionPanel.Visible = false;
        throwSelectionPanel.Visible = true;
    }

    private void OnSkipChosen()
    {
        GD.Print("Player continuing to next scene");
        ContinueToNextScene();
    }

    private void ContinueToNextScene()
    {
        // Replace this with whatever loads your next step
        GameManager.Instance.LoadNextScene();
    }

    public override void _ExitTree()
    {
        // Disconnect any remaining handlers to avoid duplicates or leaks
        // Use ToList() to avoid modifying the collection while iterating
        foreach (var kv in buttonHandlers.ToList())
        {
            var button = kv.Key;
            var handler = kv.Value;
            button.Pressed -= handler;
        }
        buttonHandlers.Clear();
    }
}
