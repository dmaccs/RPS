using Godot;
using Rps;

public partial class BattleScene : Control
{
	private TextureButton rock;
	private TextureButton paper;
	private TextureButton scissors;

	private Enemy currentEnemy = null;
	private Node currentEnemyRoot = null; // Track root node to prevent leaks
	private Player player = null;
	private BattleManager battleManager = null;

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

		GetEnemy();
	}

	// Public method so PersistentUI can access the current enemy for item usage
	public Enemy GetCurrentEnemy()
	{
		return currentEnemy;
	}

	private void OnButtonPressed(Throws playerThrow)
	{
		GD.Print("You threw: " + playerThrow);
		
		battleManager.ResolveRound(playerThrow);
		
		// if (currentEnemy.Health <= 0)
		// {
		// 	GD.Print("Enemy Defeated!");
		// 	points++;
		// 	GameManager.Instance.LoadNextScene();
		// }
		// if (player.Health <= 0)
		// {
		// 	GD.Print("You lose! Your final score was: " + points);
		// }
	}

	private void GetEnemy()
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

		// Choose which enemy to spawn — for now, random
	// Include some tougher enemies
	string[] enemyTypes = { "RockEnemy", "PaperEnemy", "ScissorsEnemy", "RandomEnemy", "CopycatEnemy", "CounterEnemy" };
		RandomNumberGenerator rng = RngManager.Instance.Rng;
		string enemyId = enemyTypes[rng.RandiRange(0, enemyTypes.Length - 1)];

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
		currentEnemy.Position = new Vector2(400, 200);
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
