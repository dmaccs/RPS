using Godot;
using Rps;

public partial class BattleScene : Control
{
	protected TextureButton rock;
	protected TextureButton paper;
	protected TextureButton scissors;

	// Combat feedback UI elements
	protected Control combatFeedbackPanel;
	protected TextureRect playerThrowIcon;
	protected TextureRect enemyThrowIcon;
	protected Label resultLabel;
	protected Label damageLabel;

	protected Enemy currentEnemy = null;
	protected Node currentEnemyRoot = null; // Track root node to prevent leaks
	protected Player player = null;
	protected BattleManager battleManager = null;

	private int points = 0;

	public override void _Ready()
	{
		player = GameManager.Instance.Player;
		rock = GetNode<TextureButton>("Rock");
		paper = GetNode<TextureButton>("Paper");
		scissors = GetNode<TextureButton>("Scissors");
		rock.Pressed += () => OnButtonPressed(Throws.rock);
		paper.Pressed += () => OnButtonPressed(Throws.paper);
		scissors.Pressed += () => OnButtonPressed(Throws.scissors);

		// Get combat feedback UI elements
		combatFeedbackPanel = GetNode<Control>("CombatFeedbackPanel");
		playerThrowIcon = GetNode<TextureRect>("CombatFeedbackPanel/HBoxContainer/PlayerThrowIcon");
		enemyThrowIcon = GetNode<TextureRect>("CombatFeedbackPanel/HBoxContainer/EnemyThrowIcon");
		resultLabel = GetNode<Label>("CombatFeedbackPanel/HBoxContainer/VBoxContainer/ResultLabel");
		damageLabel = GetNode<Label>("CombatFeedbackPanel/HBoxContainer/VBoxContainer/DamageLabel");

		// Hide feedback panel initially
		combatFeedbackPanel.Visible = false;

		GetEnemy();
	}

	// Public method so PersistentUI can access the current enemy for item usage
	public Enemy GetCurrentEnemy()
	{
		return currentEnemy;
	}

	protected void OnButtonPressed(Throws playerThrow)
	{
		GD.Print("You threw: " + playerThrow);

		var result = battleManager.ResolveRound(playerThrow);
		ShowCombatFeedback(result);

		// Check if player died
		if (player.IsDead())
		{
			GD.Print("Player died!");
			// Store killer info in GameState
			if (GameState.Instance != null && currentEnemy != null)
			{
				GameState.Instance.LastKillerName = currentEnemy.GetDisplayName();
				GameState.Instance.LastScore = 100; // Placeholder score for now
			}
			// Transition to lose scene
			GetTree().CallDeferred("change_scene_to_file", "res://Scenes/Transitions/LoseScene.tscn");
		}
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

		// Set damage text
		if (result.DamageDealt > 0)
		{
			if (result.DamageTarget == "enemy")
			{
				damageLabel.Text = $"Enemy takes {result.DamageDealt} damage!";
			}
			else
			{
				damageLabel.Text = $"You take {result.DamageDealt} damage!";
			}
		}
		else
		{
			damageLabel.Text = "No damage!";
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
