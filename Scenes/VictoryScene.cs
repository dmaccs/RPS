using Godot;
using System;

public partial class VictoryScene : Control
{
	Button MainMenuButton;
	public override void _Ready()
	{
		MainMenuButton = GetNode<Button>("VBoxContainer/Button");
		MainMenuButton.Pressed += OnMainMenuPressed;
	}

	private void OnMainMenuPressed()
	{
		GD.Print("Returning to Main Menu");
		GetTree().ChangeSceneToFile("res://Scenes/MainMenuScene.tscn");
		//GameManager.Instance.ResetGame();
	}

}
