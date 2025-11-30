using Godot;
using Rps;
using System.Linq;

public partial class InventoryScene : Control
{
	// Equipped slot panels (top row)
	private Panel[] equippedSlots = new Panel[3];
	private Label[] equippedLabels = new Label[3];

	// Inventory slot panels (bottom row)
	private Panel[] inventorySlots = new Panel[3];
	private Label[] inventoryLabels = new Label[3];

	// Drag state
	private bool isDragging = false;
	private bool dragFromEquipped = false;
	private int dragSourceIndex = -1;
	private Control dragPreview = null;

	// Discard selection
	private int selectedDiscardIndex = -1;

	// Buttons
	private Button discardButton;
	private Button closeButton;

	// Lock state label
	private Label lockedLabel;

	// Battle lock state
	private bool isLocked = false;

	// Reference to player
	private Player player;

	// Colors
	private readonly Color normalColor = new Color(1, 1, 1, 1);
	private readonly Color emptyColor = new Color(0.5f, 0.5f, 0.5f, 1);
	private readonly Color dragHighlightColor = new Color(0.5f, 1f, 0.5f, 1);
	private readonly Color invalidDropColor = new Color(1f, 0.5f, 0.5f, 1);
	private readonly Color discardSelectedColor = new Color(1f, 0.3f, 0.3f, 1);

	public override void _Ready()
	{
		player = GameManager.Instance.Player;

		// Get equipped slots
		for (int i = 0; i < 3; i++)
		{
			equippedSlots[i] = GetNode<Panel>($"MainContainer/SlotsContainer/EquippedSection/EquippedSlots/EquippedSlot{i + 1}");
			equippedLabels[i] = GetNode<Label>($"MainContainer/SlotsContainer/EquippedSection/EquippedSlots/EquippedSlot{i + 1}/Label");
		}

		// Get inventory slots
		for (int i = 0; i < 3; i++)
		{
			inventorySlots[i] = GetNode<Panel>($"MainContainer/SlotsContainer/InventorySection/InventorySlots/InventorySlot{i + 1}");
			inventoryLabels[i] = GetNode<Label>($"MainContainer/SlotsContainer/InventorySection/InventorySlots/InventorySlot{i + 1}/Label");
		}

		// Get buttons
		discardButton = GetNodeOrNull<Button>("MainContainer/ButtonContainer/DiscardButton");
		closeButton = GetNodeOrNull<Button>("MainContainer/ButtonContainer/CloseButton");

		if (discardButton != null)
			discardButton.Pressed += OnDiscardPressed;
		if (closeButton != null)
			closeButton.Pressed += OnClosePressed;

		// Hide old buttons if they exist
		var swapButton = GetNodeOrNull<Button>("MainContainer/ButtonContainer/SwapButton");
		var equipButton = GetNodeOrNull<Button>("MainContainer/ButtonContainer/EquipButton");
		var unequipButton = GetNodeOrNull<Button>("MainContainer/ButtonContainer/UnequipButton");
		if (swapButton != null) swapButton.Visible = false;
		if (equipButton != null) equipButton.Visible = false;
		if (unequipButton != null) unequipButton.Visible = false;

		// Get locked label
		lockedLabel = GetNodeOrNull<Label>("MainContainer/LockedLabel");

		// Check if in battle (lock inventory)
		CheckBattleLock();

		UpdateUI();
	}

	private void CheckBattleLock()
	{
		var currentScene = GetTree().CurrentScene;
		isLocked = currentScene is BattleScene;

		if (lockedLabel != null)
		{
			lockedLabel.Visible = isLocked;
		}

		if (discardButton != null)
			discardButton.Disabled = isLocked;
	}

	private void UpdateUI()
	{
		// Update equipped slots
		for (int i = 0; i < 3; i++)
		{
			if (player.EquippedThrows[i] != null)
			{
				var throwData = player.EquippedThrows[i];
				string attrStr = string.Join("/", throwData.Attributes.Select(a => a.ToString()[0]));
				equippedLabels[i].Text = $"{throwData.Name}\n({throwData.Effect.BaseDamage}dmg)\n[{attrStr}]";
				equippedSlots[i].Modulate = normalColor;
			}
			else
			{
				equippedLabels[i].Text = "Empty";
				equippedSlots[i].Modulate = emptyColor;
			}
		}

		// Update inventory slots
		for (int i = 0; i < 3; i++)
		{
			if (player.InventoryThrows[i] != null)
			{
				var throwData = player.InventoryThrows[i];
				string attrStr = string.Join("/", throwData.Attributes.Select(a => a.ToString()[0]));
				inventoryLabels[i].Text = $"{throwData.Name}\n({throwData.Effect.BaseDamage}dmg)\n[{attrStr}]";

				// Highlight if selected for discard
				inventorySlots[i].Modulate = (selectedDiscardIndex == i) ? discardSelectedColor : normalColor;
			}
			else
			{
				inventoryLabels[i].Text = "Empty";
				inventorySlots[i].Modulate = emptyColor;
			}
		}

		// Update discard button
		if (discardButton != null)
		{
			bool hasSelection = selectedDiscardIndex >= 0 && player.InventoryThrows[selectedDiscardIndex] != null;
			discardButton.Disabled = isLocked || !hasSelection;
			discardButton.Text = hasSelection ? "Discard Selected" : "Discard";
		}
	}

	public override void _Input(InputEvent ev)
	{
		if (isLocked)
		{
			if (ev is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
			{
				OnClosePressed();
				GetViewport().SetInputAsHandled();
			}
			return;
		}

		if (ev is InputEventMouseButton mb)
		{
			if (mb.ButtonIndex == MouseButton.Left)
			{
				if (mb.Pressed)
				{
					TryStartDrag(mb.GlobalPosition);
				}
				else if (isDragging)
				{
					TryEndDrag(mb.GlobalPosition);
				}
			}
			else if (mb.ButtonIndex == MouseButton.Right && mb.Pressed)
			{
				// Right click to select for discard
				TrySelectForDiscard(mb.GlobalPosition);
			}
		}
		else if (ev is InputEventMouseMotion mm && isDragging)
		{
			UpdateDragPreview(mm.GlobalPosition);
			UpdateDropHighlights(mm.GlobalPosition);
		}
		else if (ev is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
		{
			if (isDragging)
			{
				CancelDrag();
			}
			else
			{
				OnClosePressed();
			}
			GetViewport().SetInputAsHandled();
		}
	}

	private void TrySelectForDiscard(Vector2 globalPos)
	{
		// Check inventory slots for right-click selection
		for (int i = 0; i < 3; i++)
		{
			if (inventorySlots[i].GetGlobalRect().HasPoint(globalPos) && player.InventoryThrows[i] != null)
			{
				// Toggle selection
				if (selectedDiscardIndex == i)
					selectedDiscardIndex = -1;
				else
					selectedDiscardIndex = i;
				UpdateUI();
				return;
			}
		}
	}

	private void TryStartDrag(Vector2 globalPos)
	{
		// Check equipped slots
		for (int i = 0; i < 3; i++)
		{
			if (player.EquippedThrows[i] != null && equippedSlots[i].GetGlobalRect().HasPoint(globalPos))
			{
				StartDrag(true, i, player.EquippedThrows[i]);
				return;
			}
		}

		// Check inventory slots
		for (int i = 0; i < 3; i++)
		{
			if (player.InventoryThrows[i] != null && inventorySlots[i].GetGlobalRect().HasPoint(globalPos))
			{
				// Left click on inventory also selects for discard
				selectedDiscardIndex = i;
				StartDrag(false, i, player.InventoryThrows[i]);
				return;
			}
		}
	}

	private void StartDrag(bool fromEquipped, int index, ThrowData throwData)
	{
		isDragging = true;
		dragFromEquipped = fromEquipped;
		dragSourceIndex = index;

		// Create drag preview
		dragPreview = new Panel();
		dragPreview.CustomMinimumSize = new Vector2(100, 80);
		dragPreview.Modulate = new Color(1, 1, 1, 0.7f);

		var label = new Label();
		label.Text = throwData.Name;
		label.HorizontalAlignment = HorizontalAlignment.Center;
		label.VerticalAlignment = VerticalAlignment.Center;
		label.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		dragPreview.AddChild(label);

		AddChild(dragPreview);
		dragPreview.GlobalPosition = GetGlobalMousePosition() - dragPreview.Size / 2;

		// Dim the source slot
		if (fromEquipped)
			equippedSlots[index].Modulate = emptyColor;
		else
			inventorySlots[index].Modulate = emptyColor;
	}

	private void UpdateDragPreview(Vector2 globalPos)
	{
		if (dragPreview != null)
		{
			dragPreview.GlobalPosition = globalPos - dragPreview.Size / 2;
		}
	}

	private void UpdateDropHighlights(Vector2 globalPos)
	{
		// Reset all slot colors
		for (int i = 0; i < 3; i++)
		{
			if (player.EquippedThrows[i] != null)
				equippedSlots[i].Modulate = (dragFromEquipped && i == dragSourceIndex) ? emptyColor : normalColor;
			else
				equippedSlots[i].Modulate = emptyColor;

			if (player.InventoryThrows[i] != null)
				inventorySlots[i].Modulate = (!dragFromEquipped && i == dragSourceIndex) ? emptyColor : normalColor;
			else
				inventorySlots[i].Modulate = emptyColor;
		}

		// Highlight drop target
		for (int i = 0; i < 3; i++)
		{
			if (equippedSlots[i].GetGlobalRect().HasPoint(globalPos))
			{
				bool canDrop = CanDropAt(true, i);
				equippedSlots[i].Modulate = canDrop ? dragHighlightColor : invalidDropColor;
				return;
			}
			if (inventorySlots[i].GetGlobalRect().HasPoint(globalPos))
			{
				bool canDrop = CanDropAt(false, i);
				inventorySlots[i].Modulate = canDrop ? dragHighlightColor : invalidDropColor;
				return;
			}
		}
	}

	private bool CanDropAt(bool toEquipped, int targetIndex)
	{
		// Can't drop on same slot
		if (toEquipped == dragFromEquipped && targetIndex == dragSourceIndex)
			return false;

		if (toEquipped)
		{
			// Dropping to equipped slot - always allowed
			return true;
		}
		else
		{
			// Dropping to inventory slot
			if (dragFromEquipped)
			{
				// Equipped -> Inventory: must keep at least 1 equipped
				// Count how many equipped throws there will be after the move
				int equippedAfterMove = player.GetEquippedCount();

				// If target has a throw, it's a swap - equipped count stays same
				if (player.InventoryThrows[targetIndex] != null)
					return true;

				// If target is empty, we're moving out of equipped
				// This reduces equipped count by 1
				if (equippedAfterMove <= 1)
					return false;

				return true;
			}
			else
			{
				// Inventory -> Inventory: always allowed
				return true;
			}
		}
	}

	private void TryEndDrag(Vector2 globalPos)
	{
		// Find drop target
		for (int i = 0; i < 3; i++)
		{
			if (equippedSlots[i].GetGlobalRect().HasPoint(globalPos))
			{
				if (CanDropAt(true, i))
					PerformDrop(true, i);
				EndDrag();
				return;
			}
			if (inventorySlots[i].GetGlobalRect().HasPoint(globalPos))
			{
				if (CanDropAt(false, i))
					PerformDrop(false, i);
				EndDrag();
				return;
			}
		}

		EndDrag();
	}

	private void PerformDrop(bool toEquipped, int targetIndex)
	{
		ThrowData sourceThrow;
		ThrowData targetThrow;

		if (dragFromEquipped)
		{
			sourceThrow = player.EquippedThrows[dragSourceIndex];
			if (toEquipped)
			{
				// Equipped -> Equipped: swap within equipped
				targetThrow = player.EquippedThrows[targetIndex];
				player.EquippedThrows[dragSourceIndex] = targetThrow;
				player.EquippedThrows[targetIndex] = sourceThrow;
			}
			else
			{
				// Equipped -> Inventory: swap or move
				targetThrow = player.InventoryThrows[targetIndex];
				player.EquippedThrows[dragSourceIndex] = targetThrow;
				player.InventoryThrows[targetIndex] = sourceThrow;

				// Update discard selection to follow the moved throw
				selectedDiscardIndex = targetIndex;
			}
		}
		else
		{
			sourceThrow = player.InventoryThrows[dragSourceIndex];
			if (toEquipped)
			{
				// Inventory -> Equipped: swap or move
				targetThrow = player.EquippedThrows[targetIndex];
				player.InventoryThrows[dragSourceIndex] = targetThrow;
				player.EquippedThrows[targetIndex] = sourceThrow;

				// Update discard selection
				if (targetThrow != null)
					selectedDiscardIndex = dragSourceIndex;
				else
					selectedDiscardIndex = -1;
			}
			else
			{
				// Inventory -> Inventory: swap within inventory
				targetThrow = player.InventoryThrows[targetIndex];
				player.InventoryThrows[dragSourceIndex] = targetThrow;
				player.InventoryThrows[targetIndex] = sourceThrow;

				// Update discard selection to follow the moved throw
				selectedDiscardIndex = targetIndex;
			}
		}

		GameState.Instance?.RefreshUI();
	}

	private void CancelDrag()
	{
		EndDrag();
	}

	private void EndDrag()
	{
		isDragging = false;
		dragSourceIndex = -1;

		if (dragPreview != null)
		{
			dragPreview.QueueFree();
			dragPreview = null;
		}

		UpdateUI();
	}

	private void OnDiscardPressed()
	{
		if (isLocked) return;
		if (selectedDiscardIndex < 0 || player.InventoryThrows[selectedDiscardIndex] == null) return;

		player.DiscardThrow(selectedDiscardIndex);
		selectedDiscardIndex = -1;
		UpdateUI();
	}

	private void OnClosePressed()
	{
		QueueFree();
	}
}
