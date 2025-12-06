using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Rps;

public partial class Shop : Control
{
	private struct ShopSlot
	{
		public Button Button;
		public Label CostLabel;
		public Label ErrorLabel;
		public Timer ErrorTimer;
		public ItemData ItemData;
	}

	private struct ThrowShopSlot
	{
		public Button Button;
		public Label CostLabel;
		public Label ErrorLabel;
		public Timer ErrorTimer;
		public ThrowData ThrowData;
	}

	private List<ShopSlot> shopSlots = new();
	private List<ThrowShopSlot> throwSlots = new();
	private Button exitButton;

	// Tooltip elements
	private Panel tooltipPanel;
	private Label tooltipLabel;

	public override void _Ready()
	{
		// Create tooltip panel
		CreateTooltipPanel();
		// Initialize shop slots
		for (int i = 1; i <= 6; i++)
		{
			var slot = new ShopSlot
			{
				Button = GetNode<Button>($"VBoxContainer/HBoxContainer{i}/Button"),
				CostLabel = GetNode<Label>($"VBoxContainer/HBoxContainer{i}/Label"),
				ErrorLabel = GetNode<Label>($"VBoxContainer/HBoxContainer{i}/ErrorLabel"),
				ErrorTimer = new Timer()
			};

			// Setup timer for this slot
			slot.ErrorTimer.WaitTime = 2.0; // Show error for 2 seconds
			slot.ErrorTimer.OneShot = true;
			int slotIndex = i - 1; // Capture for lambda
			slot.ErrorTimer.Timeout += () => {
				var s = shopSlots[slotIndex];
				s.ErrorLabel.Visible = false;
				shopSlots[slotIndex] = s;
			};
			AddChild(slot.ErrorTimer);

			shopSlots.Add(slot);
		}

		// Create throw slots dynamically
		CreateThrowSlots();

		exitButton = GetNode<Button>("VBoxContainer/Button");
		exitButton.Pressed += OnExitPressed;

		// Populate shop with random items and throws
		PopulateShop();

		// Connect purchase buttons
		for (int i = 0; i < shopSlots.Count; i++)
		{
			int index = i; // Capture for lambda
			shopSlots[i].Button.Pressed += () => PurchaseItem(index);
		}

		for (int i = 0; i < throwSlots.Count; i++)
		{
			int index = i; // Capture for lambda
			throwSlots[i].Button.Pressed += () => PurchaseThrow(index);
		}
	}

	private void CreateThrowSlots()
	{
		var vbox = GetNode<VBoxContainer>("VBoxContainer");

		// Add a separator label
		var throwLabel = new Label();
		throwLabel.Text = "-- Throws --";
		vbox.AddChild(throwLabel);
		vbox.MoveChild(throwLabel, vbox.GetChildCount() - 1);

		// Create 2 throw slots
		for (int i = 0; i < 2; i++)
		{
			var hbox = new HBoxContainer();

			var costLabel = new Label();
			costLabel.Text = "0g";

			var button = new Button();
			button.Text = "Throw";

			var errorLabel = new Label();
			errorLabel.Text = "Not enough gold!";
			errorLabel.Visible = false;
			errorLabel.Modulate = new Color(1, 0.3f, 0.3f, 1);

			hbox.AddChild(costLabel);
			hbox.AddChild(button);
			hbox.AddChild(errorLabel);

			vbox.AddChild(hbox);
			vbox.MoveChild(hbox, vbox.GetChildCount() - 1);

			var slot = new ThrowShopSlot
			{
				Button = button,
				CostLabel = costLabel,
				ErrorLabel = errorLabel,
				ErrorTimer = new Timer()
			};

			slot.ErrorTimer.WaitTime = 2.0;
			slot.ErrorTimer.OneShot = true;
			int slotIndex = i;
			slot.ErrorTimer.Timeout += () => {
				var s = throwSlots[slotIndex];
				s.ErrorLabel.Visible = false;
				throwSlots[slotIndex] = s;
			};
			AddChild(slot.ErrorTimer);

			throwSlots.Add(slot);
		}
	}

	private void PopulateShop()
	{
		// Get all consumables and unseen relics
		var allConsumables = ItemDatabase.Instance.GetAllConsumables();
		var unseenRelics = ItemDatabase.Instance.GetUnseenRelics();

		// If all relics have been seen, include all relics again
		if (unseenRelics.Count == 0)
		{
			unseenRelics = ItemDatabase.Instance.GetAllRelics();
		}

		// Combine into one list
		var allItems = new List<ItemData>();
		allItems.AddRange(allConsumables);
		allItems.AddRange(unseenRelics);

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

			// Mark relics as seen when they appear in shop
			if (ItemDatabase.Instance.GetItemCategory(selectedItems[i].Id) == ItemCategory.Relic)
			{
				GameState.Instance.MarkRelicAsSeen(selectedItems[i].Id);
			}
		}

		// Populate throw slots
		PopulateThrowSlots();
	}

	private void PopulateThrowSlots()
	{
		var selectedThrows = ThrowDatabase.Instance.SelectRandomThrowsByRarity(2, RngManager.Instance?.Rng);

		for (int i = 0; i < throwSlots.Count && i < selectedThrows.Count; i++)
		{
			var slot = throwSlots[i];
			slot.ThrowData = selectedThrows[i];
			slot.Button.Text = $"{selectedThrows[i].Name} ({selectedThrows[i].Rarity})";
			slot.CostLabel.Text = $"{selectedThrows[i].Cost}g";
			throwSlots[i] = slot;
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
			ShowErrorMessage(slotIndex);
			return;
		}

		// Add item to inventory based on type
		var category = ItemDatabase.Instance.GetItemCategory(slot.ItemData.Id);
		if (category == ItemCategory.Consumable)
		{
			// Check if inventory has space
			if (!GameState.Instance.AddItem(slot.ItemData.Id))
			{
				GD.Print("Inventory full!");
				ShowErrorMessage(slotIndex, "Inventory full!");
				return;
			}

			// Deduct gold after successful addition
			GameState.Instance.AddGold(-slot.ItemData.Cost);
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

			// Deduct gold after successful addition
			GameState.Instance.AddGold(-slot.ItemData.Cost);
			GD.Print($"Purchased relic: {slot.ItemData.Name}");
		}

		// Disable button after purchase
		slot.Button.Disabled = true;
		slot.Button.Text = "[SOLD]";
	}

	private void ShowErrorMessage(int slotIndex, string message = "Not enough gold!")
	{
		if (slotIndex < 0 || slotIndex >= shopSlots.Count)
			return;

		var slot = shopSlots[slotIndex];
		slot.ErrorLabel.Text = message;
		slot.ErrorLabel.Visible = true;
		slot.ErrorTimer.Start();
		shopSlots[slotIndex] = slot;
	}

	private void PurchaseThrow(int slotIndex)
	{
		if (slotIndex < 0 || slotIndex >= throwSlots.Count)
			return;

		var slot = throwSlots[slotIndex];

		if (slot.ThrowData == null)
		{
			GD.Print("No throw in this slot");
			return;
		}

		// Check if player has enough gold
		if (GameState.Instance.PlayerGold < slot.ThrowData.Cost)
		{
			GD.Print("Not enough gold!");
			ShowThrowErrorMessage(slotIndex);
			return;
		}

		var player = GameManager.Instance.Player;
		var throwInstance = ThrowDatabase.Instance.CreateInstance(slot.ThrowData.Id);

		// Try to add to inventory first, then equipped if inventory full
		bool added = false;
		for (int i = 0; i < player.InventoryThrows.Length; i++)
		{
			if (player.InventoryThrows[i] == null)
			{
				player.SetInventorySlot(i, throwInstance);
				added = true;
				break;
			}
		}

		if (!added)
		{
			// Inventory full, try equipped slots
			for (int i = 0; i < player.EquippedThrows.Length; i++)
			{
				if (player.EquippedThrows[i] == null)
				{
					player.SetEquippedSlot(i, throwInstance);
					added = true;
					break;
				}
			}
		}

		if (!added)
		{
			GD.Print("No space for new throw!");
			ShowThrowErrorMessage(slotIndex, "Throws full!");
			return;
		}

		// Deduct gold after successful addition
		GameState.Instance.AddGold(-slot.ThrowData.Cost);
		GD.Print($"Purchased throw: {slot.ThrowData.Name}");
		GameState.Instance.RefreshUI();

		// Disable button after purchase
		slot.Button.Disabled = true;
		slot.Button.Text = "[SOLD]";
	}

	private void ShowThrowErrorMessage(int slotIndex, string message = "Not enough gold!")
	{
		if (slotIndex < 0 || slotIndex >= throwSlots.Count)
			return;

		var slot = throwSlots[slotIndex];
		slot.ErrorLabel.Text = message;
		slot.ErrorLabel.Visible = true;
		slot.ErrorTimer.Start();
		throwSlots[slotIndex] = slot;
	}

	private void CreateTooltipPanel()
	{
		// Create tooltip panel
		tooltipPanel = new Panel();
		tooltipPanel.Visible = false;
		tooltipPanel.MouseFilter = Control.MouseFilterEnum.Ignore;
		tooltipPanel.ZIndex = 100;
		AddChild(tooltipPanel);

		// Create tooltip label
		tooltipLabel = new Label();
		tooltipLabel.AutowrapMode = TextServer.AutowrapMode.Word;
		tooltipLabel.CustomMinimumSize = new Vector2(200, 0);
		tooltipPanel.AddChild(tooltipLabel);

		// Style the tooltip panel
		var styleBox = new StyleBoxFlat();
		styleBox.BgColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
		styleBox.BorderColor = new Color(0.5f, 0.5f, 0.5f, 1f);
		styleBox.SetBorderWidthAll(2);
		styleBox.SetContentMarginAll(8);
		tooltipPanel.AddThemeStyleboxOverride("panel", styleBox);
	}

	public override void _Process(double delta)
	{
		// Check if mouse is hovering over any shop item button
		Vector2 mousePos = GetGlobalMousePosition();
		bool hovering = false;

		// Check item slots
		for (int i = 0; i < shopSlots.Count; i++)
		{
			var slot = shopSlots[i];
			if (slot.Button != null && slot.ItemData != null && IsInstanceValid(slot.Button))
			{
				Rect2 buttonRect = slot.Button.GetGlobalRect();
				if (buttonRect.HasPoint(mousePos))
				{
					// Show tooltip for this item
					ShowTooltip(slot.ItemData, mousePos);
					hovering = true;
					break;
				}
			}
		}

		// Check throw slots
		if (!hovering)
		{
			for (int i = 0; i < throwSlots.Count; i++)
			{
				var slot = throwSlots[i];
				if (slot.Button != null && slot.ThrowData != null && IsInstanceValid(slot.Button))
				{
					Rect2 buttonRect = slot.Button.GetGlobalRect();
					if (buttonRect.HasPoint(mousePos))
					{
						ShowThrowTooltip(slot.ThrowData, mousePos);
						hovering = true;
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

	private void ShowTooltip(ItemData itemData, Vector2 mousePos)
	{
		if (itemData == null)
			return;

		tooltipLabel.Text = itemData.Description;
		tooltipPanel.Visible = true;

		// Position tooltip near mouse
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

	private void ShowThrowTooltip(ThrowData throwData, Vector2 mousePos)
	{
		if (throwData == null)
			return;

		tooltipLabel.Text = $"{throwData.Name} ({throwData.Rarity})\n{throwData.Description}";
		tooltipPanel.Visible = true;

		// Position tooltip near mouse
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

		// Clean up item slot error timers
		foreach (var slot in shopSlots)
		{
			if (slot.ErrorTimer != null && IsInstanceValid(slot.ErrorTimer))
			{
				slot.ErrorTimer.Stop();
				slot.ErrorTimer.QueueFree();
			}
		}

		// Clean up throw slot error timers
		foreach (var slot in throwSlots)
		{
			if (slot.ErrorTimer != null && IsInstanceValid(slot.ErrorTimer))
			{
				slot.ErrorTimer.Stop();
				slot.ErrorTimer.QueueFree();
			}
		}

		base._ExitTree();
	}
}
