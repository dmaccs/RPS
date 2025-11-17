using Godot;
using System;
using System.Collections.Generic;

public partial class EventScene : Control
{
    private Label titleLabel;
    private Label descriptionLabel;
    private ColorRect imagePanel;
    private VBoxContainer optionsContainer;
    private Label resultLabel;
    private Button leaveButton;

    private EventData currentEvent;
    private List<Button> optionButtons = new();
    private Dictionary<Button, Action> buttonHandlers = new();
    private bool outcomeApplied = false;

    public override void _Ready()
    {
        // Get UI node references
        titleLabel = GetNode<Label>("VBoxContainer/TitleLabel");
        descriptionLabel = GetNode<Label>("VBoxContainer/DescriptionLabel");
        imagePanel = GetNode<ColorRect>("VBoxContainer/ImagePanel");
        optionsContainer = GetNode<VBoxContainer>("VBoxContainer/OptionsContainer");
        resultLabel = GetNode<Label>("VBoxContainer/ResultLabel");
        leaveButton = GetNode<Button>("VBoxContainer/LeaveButton");

        // Connect leave button
        leaveButton.Pressed += OnLeavePressed;

        // Load and display random event
        LoadRandomEvent();
    }

    private void LoadRandomEvent()
    {
        if (EventDatabase.Instance == null)
        {
            GD.PrintErr("EventDatabase is not initialized!");
            return;
        }

        // Get current stage (for now always use stage 1, can be expanded later)
        int currentStage = 1;

        // Select random event for this stage
        currentEvent = EventDatabase.Instance.SelectRandomEvent(currentStage);

        if (currentEvent == null)
        {
            GD.PrintErr($"No events available for stage {currentStage}");
            return;
        }

        // Populate UI
        DisplayEvent();
    }

    private void DisplayEvent()
    {
        // Set title and description
        titleLabel.Text = currentEvent.Title;
        descriptionLabel.Text = currentEvent.Description;

        // Set image panel color
        if (!string.IsNullOrEmpty(currentEvent.ColorHex))
        {
            imagePanel.Color = new Color(currentEvent.ColorHex);
        }

        // Create option buttons
        CreateOptionButtons();
    }

    private void CreateOptionButtons()
    {
        // Clear any existing buttons
        foreach (var button in optionButtons)
        {
            if (IsInstanceValid(button))
            {
                button.QueueFree();
            }
        }
        optionButtons.Clear();
        buttonHandlers.Clear();

        // Create a button for each option
        for (int i = 0; i < currentEvent.Options.Count; i++)
        {
            var option = currentEvent.Options[i];
            var button = new Button();
            button.Text = option.Text;

            // Capture index for lambda
            int optionIndex = i;
            Action handler = () => OnOptionSelected(optionIndex);
            button.Pressed += handler;

            buttonHandlers[button] = handler;
            optionsContainer.AddChild(button);
            optionButtons.Add(button);
        }
    }

    private void OnOptionSelected(int optionIndex)
    {
        if (outcomeApplied || optionIndex < 0 || optionIndex >= currentEvent.Options.Count)
            return;

        var selectedOption = currentEvent.Options[optionIndex];
        var player = GameManager.Instance.Player;

        if (player == null)
        {
            GD.PrintErr("Player not found!");
            return;
        }

        // Apply all outcomes for this option
        var resultTexts = new List<string>();

        if (selectedOption.Outcomes != null)
        {
            foreach (var outcomeData in selectedOption.Outcomes)
            {
                var outcome = EventOutcomeFactory.Create(outcomeData.Type);
                if (outcome != null)
                {
                    outcome.Apply(player, outcomeData);
                    string resultText = outcome.GetResultText(outcomeData);
                    if (!string.IsNullOrEmpty(resultText))
                    {
                        resultTexts.Add(resultText);
                    }
                }
                else
                {
                    GD.PrintErr($"Unknown outcome type: {outcomeData.Type}");
                }
            }
        }

        // Refresh UI to show updated stats
        GameState.Instance.RefreshUI();

        // Show result
        DisplayResult(resultTexts);

        // Disable all option buttons
        foreach (var button in optionButtons)
        {
            button.Disabled = true;
        }

        // Show leave button
        leaveButton.Visible = true;

        outcomeApplied = true;
    }

    private void DisplayResult(List<string> resultTexts)
    {
        if (resultTexts.Count == 0)
        {
            resultLabel.Text = "Nothing happened.";
        }
        else
        {
            resultLabel.Text = string.Join("\n", resultTexts);
        }

        resultLabel.Visible = true;
    }

    private void OnLeavePressed()
    {
        GD.Print("Leaving event scene");
        GameManager.Instance.LoadNextScene();
    }

    public override void _ExitTree()
    {
        // Disconnect leave button
        if (leaveButton != null && IsInstanceValid(leaveButton))
        {
            leaveButton.Pressed -= OnLeavePressed;
        }

        // Disconnect all option button handlers
        foreach (var kvp in buttonHandlers)
        {
            if (IsInstanceValid(kvp.Key))
            {
                kvp.Key.Pressed -= kvp.Value;
            }
        }

        buttonHandlers.Clear();
        optionButtons.Clear();

        base._ExitTree();
    }
}
