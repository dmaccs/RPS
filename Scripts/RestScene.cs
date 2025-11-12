using Godot;
using System;

public partial class RestScene : Control
{
	Button RestButton;
	Button TrainButton;
	Button ExitButton;

	public override void _Ready()
	{
		RestButton = GetNode<Button>("VBoxContainer/HBoxContainer/Button");
		TrainButton = GetNode<Button>("VBoxContainer/HBoxContainer/Button2");
		ExitButton = GetNode<Button>("VBoxContainer/Button");
		RestButton.Pressed += OnRestPressed;
		TrainButton.Pressed += OnTrainPressed;
		ExitButton.Pressed += OnExitPressed;
	}

	private void OnRestPressed()
	{
		GD.Print("Resting... Health restored!");
		GameManager.Instance.Player.Heal(20);
	}

	private void OnTrainPressed()
	{
		GD.Print("Training... Gained experience!");
		//TODO: Implement training logic
	}
	
	private void OnExitPressed()
	{
		GD.Print("Exiting Rest Scene");
		GameManager.Instance.LoadNextScene();
	}

	public override void _ExitTree()
	{
		if (RestButton != null)
		{
			RestButton.Pressed -= OnRestPressed;
		}

		if (TrainButton != null)
		{
			TrainButton.Pressed -= OnTrainPressed;
		}

		if (ExitButton != null)
		{
			ExitButton.Pressed -= OnExitPressed;
		}

		base._ExitTree();
	}
}
