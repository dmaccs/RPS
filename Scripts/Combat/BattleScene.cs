using Godot;
using Rps;
using System.Collections.Generic;

public partial class BattleScene : Control
{
	// Dynamic throw buttons
	protected List<TextureButton> throwButtons = new List<TextureButton>();
	protected HBoxContainer throwButtonContainer;

	// Combat feedback UI elements
	protected Control combatFeedbackPanel;
	protected TextureRect playerThrowIcon;
	protected TextureRect enemyThrowIcon;
	protected Label resultLabel;
	protected Label damageLabel;
	protected Label specialMessageLabel;

	protected Enemy currentEnemy = null;
	protected Node currentEnemyRoot = null; // Track root node to prevent leaks
	protected Player player = null;
	protected BattleManager battleManager = null;

	private int points = 0;

	public override void _Ready()
	{
		player = GameManager.Instance.Player;

		// Set up dynamic throw buttons
		throwButtonContainer = GetNode<HBoxContainer>("ThrowButtonContainer");
		SetupDynamicThrowButtons();

		// Get combat feedback UI elements
		combatFeedbackPanel = GetNode<Control>("CombatFeedbackPanel");
		playerThrowIcon = GetNode<TextureRect>("CombatFeedbackPanel/HBoxContainer/PlayerThrowIcon");
		enemyThrowIcon = GetNode<TextureRect>("CombatFeedbackPanel/HBoxContainer/EnemyThrowIcon");
		resultLabel = GetNode<Label>("CombatFeedbackPanel/HBoxContainer/VBoxContainer/ResultLabel");
		damageLabel = GetNode<Label>("CombatFeedbackPanel/HBoxContainer/VBoxContainer/DamageLabel");
		specialMessageLabel = GetNodeOrNull<Label>("CombatFeedbackPanel/HBoxContainer/VBoxContainer/SpecialLabel");

		// Hide feedback panel initially
		combatFeedbackPanel.Visible = false;

		GetEnemy();
	}

	protected void SetupDynamicThrowButtons()
	{
		// Clear any existing buttons
		foreach (var btn in throwButtons)
		{
			btn.QueueFree();
		}
		throwButtons.Clear();

		// Create button for each non-null equipped throw
		for (int i = 0; i < 3; i++)
		{
			var throwData = player.EquippedThrows[i];
			if (throwData != null)
			{
				var button = CreateThrowButton(throwData, i);
				throwButtonContainer.AddChild(button);
				throwButtons.Add(button);
			}
		}
	}

	protected TextureButton CreateThrowButton(ThrowData throwData, int index)
	{
		var button = new TextureButton();
		button.Name = $"ThrowButton_{index}";

		// Get throw type from effect for icon
		var effect = ThrowEffectFactory.Create(throwData.Effect.EffectType);
		Throws throwType = effect.GetThrowType(throwData);

		// Load appropriate texture
		button.TextureNormal = LoadThrowTexture(throwType);
		button.StretchMode = TextureButton.StretchModeEnum.Scale;
		button.CustomMinimumSize = new Vector2(144, 144);

		// Add label for throw name
		var label = new Label();
		label.Text = throwData.Name;
		label.HorizontalAlignment = HorizontalAlignment.Center;
		label.Position = new Vector2(0, 148);
		button.AddChild(label);

		// Connect press event
		int capturedIndex = index;
		button.Pressed += () => OnThrowButtonPressed(capturedIndex);

		// Visual feedback on press
		button.ButtonDown += () =>
		{
			button.Modulate = new Color(0.7f, 0.7f, 0.7f, 1f);
			button.Scale = new Vector2(0.95f, 0.95f);
		};
		button.ButtonUp += () =>
		{
			button.Modulate = new Color(1f, 1f, 1f, 1f);
			button.Scale = new Vector2(1f, 1f);
		};

		return button;
	}

	protected void OnThrowButtonPressed(int index)
	{
		if (index < 0 || index >= 3 || player.EquippedThrows[index] == null)
			return;

		var throwData = player.EquippedThrows[index];

		// Get the effective throw (accounting for transformations)
		var effectiveThrow = battleManager.GetEffectiveThrowData(throwData);
		GD.Print($"You threw: {effectiveThrow.Name}");

		var result = battleManager.ResolveRound(throwData);
		ShowCombatFeedback(result);

		// Refresh throw buttons to show any transformations
		RefreshThrowButtonLabels();

		CheckPlayerDeath();
	}

	protected void RefreshThrowButtonLabels()
	{
		int buttonIndex = 0;
		for (int i = 0; i < 3; i++)
		{
			var throwData = player.EquippedThrows[i];
			if (throwData != null && buttonIndex < throwButtons.Count)
			{
				var button = throwButtons[buttonIndex];

				// Get the effective throw data (accounting for transformations)
				var effectiveThrow = battleManager.GetEffectiveThrowData(throwData);

				// Update the label
				var label = button.GetNodeOrNull<Label>("Label");
				if (label == null)
				{
					// Try to find label by iterating children
					foreach (var child in button.GetChildren())
					{
						if (child is Label l)
						{
							label = l;
							break;
						}
					}
				}

				if (label != null && effectiveThrow != null)
				{
					label.Text = effectiveThrow.Name;
				}

				// Update the texture if the throw type changed
				if (effectiveThrow != null)
				{
					var effect = ThrowEffectFactory.Create(effectiveThrow.Effect.EffectType);
					Throws throwType = effect.GetThrowType(effectiveThrow);
					button.TextureNormal = LoadThrowTexture(throwType);
				}

				buttonIndex++;
			}
		}
	}

	protected void CheckPlayerDeath()
	{
		if (player.IsDead())
		{
			GD.Print("Player died!");
			if (GameState.Instance != null && currentEnemy != null)
			{
				GameState.Instance.LastKillerName = currentEnemy.GetDisplayName();
				GameState.Instance.LastScore = 100;
			}
			GetTree().CallDeferred("change_scene_to_file", "res://Scenes/Transitions/LoseScene.tscn");
		}
	}

	// Public method so PersistentUI can access the current enemy for item usage
	public Enemy GetCurrentEnemy()
	{
		return currentEnemy;
	}

	protected void ShowCombatFeedback(RoundResult result)
	{
		// Load and display throw textures
		playerThrowIcon.Texture = LoadThrowTexture(result.PlayerThrow);
		enemyThrowIcon.Texture = LoadThrowTexture(result.EnemyThrow);

		// Set result text and color
		switch (result.Outcome)
		{
			case RoundOutcome.PlayerWin:
				resultLabel.Text = "You Win!";
				resultLabel.Modulate = new Color(0, 1, 0); // Green
				break;
			case RoundOutcome.EnemyWin:
				resultLabel.Text = "Enemy Wins!";
				resultLabel.Modulate = new Color(1, 0, 0); // Red
				break;
			case RoundOutcome.Draw:
				resultLabel.Text = "Draw!";
				resultLabel.Modulate = new Color(1, 1, 0); // Yellow
				break;
		}

		// Set damage text - both sides can take damage each round
		var damageLines = new List<string>();

		if (result.DamageDealt > 0)
			damageLines.Add($"Enemy takes {result.DamageDealt} damage!");

		if (result.DamageTaken > 0)
			damageLines.Add($"You take {result.DamageTaken} damage!");

		if (result.HealAmount > 0)
			damageLines.Add($"You heal {result.HealAmount} HP!");

		if (damageLines.Count == 0)
			damageLines.Add("No damage!");

		damageLabel.Text = string.Join("\n", damageLines);

		// Show special message from effect if present
		if (specialMessageLabel != null)
		{
			if (!string.IsNullOrEmpty(result.SpecialMessage))
			{
				specialMessageLabel.Text = result.SpecialMessage;
				specialMessageLabel.Visible = true;
			}
			else
			{
				specialMessageLabel.Visible = false;
			}
		}

		// Show the feedback panel
		combatFeedbackPanel.Visible = true;
	}

	protected Texture2D LoadThrowTexture(Throws throwType)
	{
		string texturePath = throwType switch
		{
			Throws.rock => "res://art/Rock.png",
			Throws.paper => "res://art/scroll.png",
			Throws.scissors => "res://art/Sciz.png",
			_ => "res://art/Rock.png"
		};

		return GD.Load<Texture2D>(texturePath);
	}

	protected void GetEnemy()
	{
		// Remove old enemy nodes if they exist
		if (currentEnemy != null)
		{
			currentEnemy.FightEndSignal -= FightOver;
		}

		if (currentEnemyRoot != null && currentEnemyRoot.IsInsideTree())
		{
			currentEnemyRoot.QueueFree();
			currentEnemyRoot = null;
		}

		currentEnemy = null;

		// Get enemy based on current battle progression
		string enemyId;
		if (BattleProgressionManager.Instance != null)
		{
			enemyId = BattleProgressionManager.Instance.GetEnemyForCurrentBattle();
		}
		else
		{
			// Fallback if BattleProgressionManager not loaded
			GD.PrintErr("BattleProgressionManager not found, using fallback enemy selection");
			string[] fallbackEnemies = { "RockEnemy", "PaperEnemy", "ScissorsEnemy" };
			enemyId = fallbackEnemies[RngManager.Instance.Rng.RandiRange(0, fallbackEnemies.Length - 1)];
		}

		// Instance enemy PackedScene so visuals and child nodes exist
		var scenePath = $"res://Scenes/Enemies/{enemyId}.tscn";
		var packed = GD.Load<PackedScene>(scenePath);
		if (packed == null)
		{
			GD.PrintErr($"Failed to load enemy scene: {scenePath}");
			// Fallback: construct a bare Enemy (no visuals) and initialize it
			currentEnemy = new Enemy();
			currentEnemy.Initialize(enemyId);
			AddChild(currentEnemy);
			currentEnemyRoot = currentEnemy; // Track the root node
		}
		else
		{
			var inst = packed.Instantiate();
			AddChild(inst);
			currentEnemyRoot = inst; // Track the root node

			// The script may be attached to the root or a child; try to get the Enemy instance
			currentEnemy = inst as Enemy;
			if (currentEnemy == null)
			{
				currentEnemy = inst.GetNodeOrNull<Enemy>("Enemy");
			}
			if (currentEnemy == null)
			{
				GD.PrintErr($"Instanced scene did not contain an Enemy node: {scenePath}");
				return;
			}
			currentEnemy.Initialize(enemyId);
		}

		// Position the enemy and wire up signals
		currentEnemy.Position = new Vector2(576, 120);
		currentEnemy.FightEndSignal += FightOver;
		GD.Print($"Spawned {enemyId}");

		// Create new battle manager for this enemy
		battleManager = new BattleManager(player, currentEnemy);
	}

	public void FightOver(bool result)
    {
		GD.Print(result);
		GameManager.Instance.LoadNextScene();
    }

	public override void _ExitTree()
	{
		// Disconnect enemy signal
		if (currentEnemy != null)
		{
			currentEnemy.FightEndSignal -= FightOver;
		}

		// Clean up the root node to prevent leaks
		if (currentEnemyRoot != null && currentEnemyRoot.IsInsideTree())
		{
			currentEnemyRoot.QueueFree();
		}

		base._ExitTree();
	}
}
