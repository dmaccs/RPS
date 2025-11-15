using Godot;
using System;
using System.Collections.Generic;
using Rps;

public partial class RestScene : Control
{
	private Button restButton;
	private Button trainButton;
	private Button exitButton;
	private Control throwSelectionPanel;

	// Throw upgrade buttons
	private Button[] throwUpgradeButtons = new Button[3];
	private Dictionary<Button, Action> buttonHandlers = new Dictionary<Button, Action>();

	public override void _Ready()
	{
		restButton = GetNode<Button>("VBoxContainer/HBoxContainer/Button");
		trainButton = GetNode<Button>("VBoxContainer/HBoxContainer/Button2");
		exitButton = GetNode<Button>("VBoxContainer/Button");

		// Get throw selection panel and buttons
		throwSelectionPanel = GetNode<Control>("ThrowSelectionPanel");
		throwUpgradeButtons[0] = GetNode<Button>("ThrowSelectionPanel/VBoxContainer/ThrowButton1");
		throwUpgradeButtons[1] = GetNode<Button>("ThrowSelectionPanel/VBoxContainer/ThrowButton2");
		throwUpgradeButtons[2] = GetNode<Button>("ThrowSelectionPanel/VBoxContainer/ThrowButton3");

		// Hide throw selection panel initially
		throwSelectionPanel.Visible = false;

		restButton.Pressed += OnRestPressed;
		trainButton.Pressed += OnTrainPressed;
		exitButton.Pressed += OnExitPressed;
	}

	private void OnRestPressed()
	{
		// Heal 30% of max health
		int healAmount = (int)(GameState.Instance.MaxPlayerHealth * 0.3f);
		GameState.Instance.ModifyHealth(healAmount);
		GD.Print($"Resting... Restored {healAmount} health!");

		// Disable both choices
		DisableChoiceButtons();
	}

	private void OnTrainPressed()
	{
		GD.Print("Training selected!");

		// Hide main buttons and show throw selection
		restButton.Visible = false;
		trainButton.Visible = false;

		// Update and show throw buttons
		UpdateThrowButtons();
		throwSelectionPanel.Visible = true;
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
				button.Text = $"Train {move.Type} (Level {move.Level})";
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

	private void OnThrowSelected(Throws throwType)
	{
		GameManager.Instance.Player.UpgradeThrow(throwType);
		GD.Print($"Trained {throwType} throw!");

		// Hide throw selection panel
		throwSelectionPanel.Visible = false;

		// Disable both choice buttons
		DisableChoiceButtons();
	}

	private void DisableChoiceButtons()
	{
		restButton.Disabled = true;
		trainButton.Disabled = true;
		restButton.Visible = true;  // Make sure they're visible but disabled
		trainButton.Visible = true;
	}

	private void OnExitPressed()
	{
		GD.Print("Exiting Rest Scene");
		GameManager.Instance.LoadNextScene();
	}

	public override void _ExitTree()
	{
		if (restButton != null)
		{
			restButton.Pressed -= OnRestPressed;
		}

		if (trainButton != null)
		{
			trainButton.Pressed -= OnTrainPressed;
		}

		if (exitButton != null)
		{
			exitButton.Pressed -= OnExitPressed;
		}

		// Disconnect throw button handlers
		foreach (var kv in buttonHandlers)
		{
			var button = kv.Key;
			var handler = kv.Value;
			if (button != null)
			{
				button.Pressed -= handler;
			}
		}
		buttonHandlers.Clear();

		base._ExitTree();
	}
}
