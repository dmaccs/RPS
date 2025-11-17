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

    // Static throw upgrade buttons
    private Button[] throwUpgradeButtons = new Button[3];
    private Dictionary<Button, Action> buttonHandlers = new Dictionary<Button, Action>();

    private Label goldLabel;

    // Random reward data
    private RewardType randomRewardType;
    private object randomRewardData;

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
        throwUpgradeButtons[0] = GetNode<Button>("ThrowSelectionPanel/VBoxContainer/ThrowButton1");
        throwUpgradeButtons[1] = GetNode<Button>("ThrowSelectionPanel/VBoxContainer/ThrowButton2");
        throwUpgradeButtons[2] = GetNode<Button>("ThrowSelectionPanel/VBoxContainer/ThrowButton3");

        // Hide throw selection panel initially
        throwSelectionPanel.Visible = false;

        // Award random base gold (5-15)
        int baseGold = RngManager.Instance.Rng.RandiRange(5, 15);
        GameState.Instance.AddGold(baseGold);
        goldLabel.Text = $"You received {baseGold} Gold!";

        // Generate random reward
        GenerateRandomReward();

        // Set button texts
        button1.Text = "Level up a throw";
        button2.Text = GetRandomRewardDescription();
        skipButton.Text = "Skip";

        // Update button states based on inventory
        UpdateRewardButtonStates();

        button1.Pressed += OnUpgradeChosen;
        button2.Pressed += OnRandomRewardChosen;
        skipButton.Pressed += OnSkipChosen;
    }

    public override void _Process(double delta)
    {
        // Update button states each frame to react to inventory changes
        UpdateRewardButtonStates();
    }

    private void UpdateRewardButtonStates()
    {
        // Check if random reward is an item and inventory is full
        if (randomRewardType == RewardType.Item)
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
        var currentThrows = GameManager.Instance.Player.CurrentThrows;
        
        // Update each button
        for (int i = 0; i < throwUpgradeButtons.Length; i++)
        {
            var button = throwUpgradeButtons[i];
            
            // If we have a throw for this slot
            if (i < currentThrows.Count)
            {
                var move = currentThrows[i];
                button.Text = $"Upgrade {move.Type} (Level {move.Level})";
                button.Visible = true;
                
                // Remove old handler if it exists
                if (buttonHandlers.TryGetValue(button, out var oldHandler))
                {
                    button.Pressed -= oldHandler;
                    buttonHandlers.Remove(button);
                }
                
                // Create and store new handler
                Action newHandler = () => OnThrowSelected(move.Type);
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

    private void OnUpgradeChosen()
    {
        // Hide main buttons and show throw selection
        button1.Visible = false;
        button2.Visible = false;
        skipButton.Visible = false;

        // Update the throw buttons
        UpdateThrowButtons();

        throwSelectionPanel.Visible = true;

        GD.Print("Player chose to level up a throw");
    }

    private void OnRandomRewardChosen()
    {
        ApplyRandomReward();
        ContinueToNextScene();
    }

    private void OnThrowSelected(Throws throwType)
    {
        GameManager.Instance.Player.UpgradeThrow(throwType);
        GD.Print($"Player upgraded {throwType} throw");
        ContinueToNextScene();
    }

    private void OnSkipChosen()
    {
        GD.Print("Player skipped reward");
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
