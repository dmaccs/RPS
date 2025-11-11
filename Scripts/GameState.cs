using Godot;
using System.Collections.Generic;

public partial class GameState : Node
{
    public static GameState Instance { get; private set; }

    // Player stats
    public int PlayerHealth { get; private set; } = 10;
    public int MaxPlayerHealth { get; private set; } = 10;
    // No global player strength; move strength is tracked per MoveData
    public int PlayerGold { get; private set; } = 0;

    // Inventory
    private List<string> items = new();
    private List<string> relics = new();

    // Persistent UI instance
    private PersistentUI persistentUI;

    public override void _Ready()
    {
        Instance = this;

        // Load and instantiate the persistent UI scene
        var uiScene = ResourceLoader.Load<PackedScene>("res://Scenes/PersistentUI.tscn");
        if (uiScene != null)
        {
            persistentUI = uiScene.Instantiate<PersistentUI>();
            AddChild(persistentUI);
        }

        UpdateUI();
    }

    // Methods to modify player stats
    public void ModifyHealth(int amount)
    {
        int old = PlayerHealth;
        PlayerHealth = Mathf.Clamp(PlayerHealth + amount, 0, MaxPlayerHealth);
        GD.Print($"GameState.ModifyHealth called: amount={amount}, old={old}, new={PlayerHealth}");
        UpdateUI();
    }

    public void SetMaxHealth(int value)
    {
        MaxPlayerHealth = value;
        PlayerHealth = Mathf.Min(PlayerHealth, MaxPlayerHealth);
        UpdateUI();
    }

    // Remove ModifyStrength — move levels are changed on Player.CurrentThrows and should call RefreshUI

    public void AddGold(int amount)
    {
        PlayerGold += amount;
        UpdateUI();
    }

    // Inventory management
    public void AddItem(string itemId)
    {
        items.Add(itemId);
        UpdateUI();
    }

    public void RemoveItem(string itemId)
    {
        items.Remove(itemId);
        UpdateUI();
    }

    public void AddRelic(string relicId)
    {
        relics.Add(relicId);
        UpdateUI();
    }

    public bool HasItem(string itemId)
    {
        return items.Contains(itemId);
    }

    public bool HasRelic(string relicId)
    {
        return relics.Contains(relicId);
    }

    private void UpdateUI()
    {
        if (persistentUI != null)
            persistentUI.UpdateUI();
    }

    // Public wrapper so gameplay code can request a UI refresh
    public void RefreshUI()
    {
        UpdateUI();
    }
}
