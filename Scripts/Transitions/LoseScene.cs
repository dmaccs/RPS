using Godot;

public partial class LoseScene : Control
{
	private Button playAgainButton;
	private Button mainMenuButton;
	private Label killedByLabel;
	private Label scoreLabel;

	public override void _Ready()
	{
		playAgainButton = GetNode<Button>("VBoxContainer/PlayAgainButton");
		mainMenuButton = GetNode<Button>("VBoxContainer/MainMenuButton");
		killedByLabel = GetNode<Label>("VBoxContainer/KilledByLabel");
		scoreLabel = GetNode<Label>("VBoxContainer/ScoreLabel");

		playAgainButton.Pressed += OnPlayAgainPressed;
		mainMenuButton.Pressed += OnMainMenuPressed;

		// Update labels with game data
		if (GameState.Instance != null)
		{
			string killerName = GameState.Instance.LastKillerName ?? "Unknown Enemy";
			int score = GameState.Instance.LastScore;

			killedByLabel.Text = $"Killed by: {killerName}";
			scoreLabel.Text = $"Score: {score}";
		}
		else
		{
			// Fallback values
			killedByLabel.Text = "Killed by: Unknown Enemy";
			scoreLabel.Text = "Score: 100";
		}
	}

	private void OnPlayAgainPressed()
	{
		GD.Print("Restarting game");
		GameState.Instance?.ResetGame();
		GameManager.Instance?.ResetGame();
	}

	private void OnMainMenuPressed()
	{
		GD.Print("Returning to Main Menu");
		GetTree().ChangeSceneToFile("res://Scenes/Transitions/MainMenuScene.tscn");
	}

	public override void _ExitTree()
	{
		if (playAgainButton != null)
		{
			playAgainButton.Pressed -= OnPlayAgainPressed;
		}
		if (mainMenuButton != null)
		{
			mainMenuButton.Pressed -= OnMainMenuPressed;
		}

		base._ExitTree();
	}
}
