using Godot;
using System.Collections.Generic;

public partial class GameState : Node
{
    public static GameState Instance { get; private set; }

    // Player stats
    public int PlayerHealth { get; private set; } = 10;
    public int MaxPlayerHealth { get; private set; } = 10;
    // No global player strength; move strength is tracked per MoveData
    public int PlayerGold { get; private set; } = 50;

    // Game over tracking
    public string LastKillerName { get; set; } = "";
    public int LastScore { get; set; } = 100;

    // Inventory
    private List<string> items = new();
    private List<string> relics = new();

    // Buff system
    // Temporary buffs (buff_type -> (amount, turns_remaining))
    private Dictionary<string, (int amount, int turnsRemaining)> temporaryBuffs = new();
    // Permanent buffs (buff_type -> amount)
    private Dictionary<string, int> permanentBuffs = new();

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

    // Buff management
    public void AddBuff(string buffType, int amount, int duration)
    {
        temporaryBuffs[buffType] = (amount, duration);
        GD.Print($"Added temporary buff: {buffType} (+{amount}) for {duration} turns");
        UpdateUI();
    }

    public void AddPermanentBuff(string buffType, int amount)
    {
        if (permanentBuffs.ContainsKey(buffType))
            permanentBuffs[buffType] += amount;
        else
            permanentBuffs[buffType] = amount;

        GD.Print($"Added permanent buff: {buffType} (+{amount})");
        UpdateUI();
    }

    public int GetBuffAmount(string buffType)
    {
        int total = 0;

        // Add temporary buff
        if (temporaryBuffs.TryGetValue(buffType, out var tempBuff))
            total += tempBuff.amount;

        // Add permanent buff
        if (permanentBuffs.TryGetValue(buffType, out int permBuff))
            total += permBuff;

        return total;
    }

    public void DecrementBuffDurations()
    {
        var expiredBuffs = new List<string>();

        foreach (var kvp in temporaryBuffs)
        {
            var buffType = kvp.Key;
            var (amount, turnsRemaining) = kvp.Value;

            int newDuration = turnsRemaining - 1;
            if (newDuration <= 0)
            {
                expiredBuffs.Add(buffType);
                GD.Print($"Buff expired: {buffType}");
            }
            else
            {
                temporaryBuffs[buffType] = (amount, newDuration);
            }
        }

        // Remove expired buffs
        foreach (var buffType in expiredBuffs)
        {
            temporaryBuffs.Remove(buffType);
        }

        if (expiredBuffs.Count > 0)
            UpdateUI();
    }

    public void IncreaseMaxHealth(int amount)
    {
        MaxPlayerHealth += amount;
        PlayerHealth += amount; // Also heal for the amount
        UpdateUI();
    }

    // Get inventory lists for UI
    public List<string> GetItems()
    {
        return new List<string>(items);
    }

    public List<string> GetRelics()
    {
        return new List<string>(relics);
    }

    // Reset game state for new run
    public void ResetGame()
    {
        PlayerHealth = 10;
        MaxPlayerHealth = 10;
        PlayerGold = 50;
        items.Clear();
        relics.Clear();
        temporaryBuffs.Clear();
        permanentBuffs.Clear();
        LastKillerName = "";
        LastScore = 100;
        UpdateUI();
    }
}
