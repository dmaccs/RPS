using Godot;

public partial class BossScene : BattleScene
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
    {
		// Override base._Ready() to customize enemy spawning
		player = GameManager.Instance.Player;
		rock = GetNode<TextureButton>("Rock");
		paper = GetNode<TextureButton>("Paper");
		scissors = GetNode<TextureButton>("Scissors");
		rock.Pressed += () => OnButtonPressed(Rps.Throws.rock);
		paper.Pressed += () => OnButtonPressed(Rps.Throws.paper);
		scissors.Pressed += () => OnButtonPressed(Rps.Throws.scissors);

		// Get combat feedback UI elements
		combatFeedbackPanel = GetNode<Control>("CombatFeedbackPanel");
		playerThrowIcon = GetNode<TextureRect>("CombatFeedbackPanel/HBoxContainer/PlayerThrowIcon");
		enemyThrowIcon = GetNode<TextureRect>("CombatFeedbackPanel/HBoxContainer/EnemyThrowIcon");
		resultLabel = GetNode<Label>("CombatFeedbackPanel/HBoxContainer/VBoxContainer/ResultLabel");
		damageLabel = GetNode<Label>("CombatFeedbackPanel/HBoxContainer/VBoxContainer/DamageLabel");

		// Hide feedback panel initially
		combatFeedbackPanel.Visible = false;

		// Spawn boss enemy
		SpawnBossEnemy();
    }

	private void SpawnBossEnemy()
	{
		string enemyId = "BossEnemy";

		// Instance enemy PackedScene
		var scenePath = $"res://Scenes/Enemies/{enemyId}.tscn";
		var packed = GD.Load<PackedScene>(scenePath);

		if (packed == null)
		{
			GD.PrintErr($"Failed to load boss enemy scene: {scenePath}, trying BaseEnemy");
			// Fallback: use BaseEnemy and initialize as BossEnemy
			scenePath = "res://Scenes/Enemies/BaseEnemy.tscn";
			packed = GD.Load<PackedScene>(scenePath);
		}

		if (packed != null)
		{
			var inst = packed.Instantiate();
			AddChild(inst);
			currentEnemyRoot = inst;

			currentEnemy = inst as Enemy;
			if (currentEnemy == null)
			{
				currentEnemy = inst.GetNodeOrNull<Enemy>("Enemy");
			}
			if (currentEnemy != null)
			{
				currentEnemy.Initialize(enemyId);
				currentEnemy.Position = new Vector2(576, 120);
				currentEnemy.FightEndSignal += FightOver;
				GD.Print($"Spawned {enemyId}");
				battleManager = new BattleManager(player, currentEnemy);
			}
		}
	}

}
