using Godot;
using System;

public partial class MainMenu : Control
{
	private Button startButton;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
    {
		startButton = GetNode<Button>("Button");
		startButton.Pressed += OnStartButtonPressed;
    }

	private void OnStartButtonPressed()	
	{
		GD.Print("Start Button Pressed");
		GameManager.Instance.ResetGame();
	}

}
