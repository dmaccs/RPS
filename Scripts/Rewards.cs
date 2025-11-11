using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Rps;

public partial class Rewards : Control
{
    private Button button1;
    private Button button2;
    private Button skipButton;
    private Control throwSelectionPanel;
    
    // Static throw upgrade buttons
    private Button[] throwUpgradeButtons = new Button[3];
    private Dictionary<Button, Action> buttonHandlers = new Dictionary<Button, Action>();
    
    private Label goldLabel;

    public override void _Ready()
    {
        goldLabel = GetNode<Label>("VBoxContainer/BoxContainer/Label2");
        button1 = GetNode<Button>("VBoxContainer/BoxContainer/Button");
        button2 = GetNode<Button>("VBoxContainer/BoxContainer/Button2");
        skipButton = GetNode<Button>("VBoxContainer/Button3");
        
        // Get throw selection panel and buttons
        throwSelectionPanel = GetNode<Control>("ThrowSelectionPanel");
        throwUpgradeButtons[0] = GetNode<Button>("ThrowSelectionPanel/VBoxContainer/ThrowButton1");
        throwUpgradeButtons[1] = GetNode<Button>("ThrowSelectionPanel/VBoxContainer/ThrowButton2");
        throwUpgradeButtons[2] = GetNode<Button>("ThrowSelectionPanel/VBoxContainer/ThrowButton3");

        // Example values — these could be set dynamically before showing this scene
        goldLabel.Text = "10 Gold and:";
        button1.Text = "Level up a throw";
        button2.Text = "Get other reward!";
        skipButton.Text = "Skip";

        // Hide throw selection panel initially
        throwSelectionPanel.Visible = false;

        button1.Pressed += OnUpgradeChosen;
        button2.Pressed += OnOtherChosen;
        skipButton.Pressed += OnSkipChosen;
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
                button.Text = $"Upgrade {move.Type} (Level {move.Level})";
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

    private void OnUpgradeChosen()
    {
        // Hide main buttons and show throw selection
        button1.Visible = false;
        button2.Visible = false;
        skipButton.Visible = false;
        
        // Update the throw buttons
        UpdateThrowButtons();
        
        throwSelectionPanel.Visible = true;
        
        GameManager.Instance.Player.AddGold(10);
        GD.Print("Player received +10 Gold");
    }

    private void OnOtherChosen()
    {
        GameManager.Instance.Player.AddGold(20); // Example of other reward
        GD.Print("Player received +20 Gold");
        ContinueToNextScene();
    }

    private void OnThrowSelected(Throws throwType)
    {
        GameManager.Instance.Player.UpgradeThrow(throwType);
        GD.Print($"Player upgraded {throwType} throw");
        ContinueToNextScene();
    }

    private void OnSkipChosen()
    {
        GD.Print("Player skipped reward");
        ContinueToNextScene();
    }

    private void ContinueToNextScene()
    {
        // Replace this with whatever loads your next step
        GameManager.Instance.LoadNextScene();
    }

    public override void _ExitTree()
    {
        // Disconnect any remaining handlers to avoid duplicates or leaks
        // Use ToList() to avoid modifying the collection while iterating
        foreach (var kv in buttonHandlers.ToList())
        {
            var button = kv.Key;
            var handler = kv.Value;
            button.Pressed -= handler;
        }
        buttonHandlers.Clear();
    }
}
