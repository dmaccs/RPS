using Godot;
using System;

public partial class Shop : Control
{
	Button ItemOne;
	Button ItemTwo;
	Button ItemThree;
	Button ItemFour;
	Button ItemFive;
	Button ItemSix;
	Button ExitButton;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		ItemOne = GetNode<Button>("VBoxContainer/HBoxContainer1/Button");
		ItemTwo = GetNode<Button>("VBoxContainer/HBoxContainer2/Button");
		ItemThree = GetNode<Button>("VBoxContainer/HBoxContainer3/Button");
		ItemFour = GetNode<Button>("VBoxContainer/HBoxContainer4/Button");
		ItemFive = GetNode<Button>("VBoxContainer/HBoxContainer5/Button");
		ItemSix = GetNode<Button>("VBoxContainer/HBoxContainer6/Button");
		ExitButton = GetNode<Button>("VBoxContainer/Button");
		ItemOne.Pressed += () => PurchaseItem(ItemOne);
		ItemTwo.Pressed += () => PurchaseItem(ItemTwo);
		ItemThree.Pressed += () => PurchaseItem(ItemThree);
		ItemFour.Pressed += () => PurchaseItem(ItemFour);
		ItemFive.Pressed += () => PurchaseItem(ItemFive);
		ItemSix.Pressed += () => PurchaseItem(ItemSix);
		ExitButton.Pressed += OnExitPressed;
	}

	private void PurchaseItem(Button itemButton)
	{
		GD.Print("Purchased: " + itemButton.Text);
	}
	private void OnExitPressed()
	{
		GD.Print("Exiting Shop");
		GameManager.Instance.LoadNextScene();
	}
};
