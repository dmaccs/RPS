using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Shop : Control
{
	private struct ShopSlot
	{
		public Button Button;
		public Label CostLabel;
		public ItemData ItemData;
	}

	private List<ShopSlot> shopSlots = new();
	private Button exitButton;

	public override void _Ready()
	{
		// Initialize shop slots
		for (int i = 1; i <= 6; i++)
		{
			var slot = new ShopSlot
			{
				Button = GetNode<Button>($"VBoxContainer/HBoxContainer{i}/Button"),
				CostLabel = GetNode<Label>($"VBoxContainer/HBoxContainer{i}/Label")
			};
			shopSlots.Add(slot);
		}

		exitButton = GetNode<Button>("VBoxContainer/Button");
		exitButton.Pressed += OnExitPressed;

		// Populate shop with random items
		PopulateShop();

		// Connect purchase buttons
		for (int i = 0; i < shopSlots.Count; i++)
		{
			int index = i; // Capture for lambda
			shopSlots[i].Button.Pressed += () => PurchaseItem(index);
		}
	}

	private void PopulateShop()
	{
		// Get all items from database
		var allItems = ItemDatabase.Instance.GetAllItems();

		if (allItems.Count == 0)
		{
			GD.PrintErr("No items found in ItemDatabase!");
			return;
		}

		// Select 6 random items weighted by rarity
		var selectedItems = SelectWeightedRandomItems(allItems, 6);

		// Populate each shop slot
		for (int i = 0; i < shopSlots.Count && i < selectedItems.Count; i++)
		{
			var slot = shopSlots[i];
			slot.ItemData = selectedItems[i];
			slot.Button.Text = selectedItems[i].Name;
			slot.CostLabel.Text = $"{selectedItems[i].Cost}g";
			shopSlots[i] = slot;
		}
	}

	private List<ItemData> SelectWeightedRandomItems(List<ItemData> items, int count)
	{
		var rng = RngManager.Instance?.Rng ?? new RandomNumberGenerator();
		var selected = new List<ItemData>();
		var availableItems = new List<ItemData>(items);

		// Rarity weights: Common = 60%, Uncommon = 30%, Rare = 10%
		var rarityWeights = new Dictionary<ItemRarity, float>
		{
			{ ItemRarity.Common, 0.6f },
			{ ItemRarity.Uncommon, 0.3f },
			{ ItemRarity.Rare, 0.1f }
		};

		for (int i = 0; i < count && availableItems.Count > 0; i++)
		{
			// Calculate total weight
			float totalWeight = 0f;
			foreach (var item in availableItems)
			{
				totalWeight += rarityWeights[item.Rarity];
			}

			// Select random item based on weight
			float randomValue = rng.Randf() * totalWeight;
			float cumulative = 0f;

			ItemData selectedItem = availableItems[0];
			foreach (var item in availableItems)
			{
				cumulative += rarityWeights[item.Rarity];
				if (randomValue <= cumulative)
				{
					selectedItem = item;
					break;
				}
			}

			selected.Add(selectedItem);
			availableItems.Remove(selectedItem); // Prevent duplicates
		}

		return selected;
	}

	private void PurchaseItem(int slotIndex)
	{
		var slot = shopSlots[slotIndex];

		if (slot.ItemData == null)
		{
			GD.Print("No item in this slot");
			return;
		}

		// Check if player has enough gold
		if (GameState.Instance.PlayerGold < slot.ItemData.Cost)
		{
			GD.Print("Not enough gold!");
			// TODO: Show feedback to player
			return;
		}

		// Deduct gold
		GameState.Instance.AddGold(-slot.ItemData.Cost);

		// Add item to inventory based on type
		var category = ItemDatabase.Instance.GetItemCategory(slot.ItemData.Id);
		if (category == ItemCategory.Consumable)
		{
			GameState.Instance.AddItem(slot.ItemData.Id);
			GD.Print($"Purchased consumable: {slot.ItemData.Name}");
		}
		else if (category == ItemCategory.Relic)
		{
			GameState.Instance.AddRelic(slot.ItemData.Id);

			// Apply relic effect immediately
			var relic = ItemDatabase.Instance.GetRelic(slot.ItemData.Id);
			if (relic?.Effect != null)
			{
				var effect = ItemEffectFactory.Create(relic.Effect.EffectType);
				effect?.Apply(GameManager.Instance.Player, null, relic.Effect);
			}

			GD.Print($"Purchased relic: {slot.ItemData.Name}");
		}

		// Disable button after purchase
		slot.Button.Disabled = true;
		slot.Button.Text = "[SOLD]";
	}

	private void OnExitPressed()
	{
		GD.Print("Exiting Shop");
		GameManager.Instance.LoadNextScene();
	}

	public override void _ExitTree()
	{
		// Disconnect all button signals to prevent leaks
		if (exitButton != null)
		{
			exitButton.Pressed -= OnExitPressed;
		}

		// Note: We can't easily disconnect lambda signals, but the buttons will be freed anyway
		// The struct's buttons are part of the scene tree and will be properly freed

		base._ExitTree();
	}
}
