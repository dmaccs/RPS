using Godot;
using System.Collections.Generic;

public partial class GameState : Node
{
    public static GameState Instance { get; private set; }

    // Starting values - configurable per difficulty/character
    public int StartingHealth { get; set; } = 90;
    public int StartingMaxHealth { get; set; } = 90;
    public int StartingGold { get; set; } = 25;
    public int DefaultScore { get; set; } = 100;

    // Player stats
    public int PlayerHealth { get; private set; } = 90;
    public int MaxPlayerHealth { get; private set; } = 90;
    public int PlayerGold { get; private set; } = 25;

    // Game over tracking
    public string LastKillerName { get; set; } = "";
    public int LastScore { get; set; } = 100;

    // Inventory
    private List<string> items = new();
    private List<string> relics = new();

    // Track which relics have been seen this run (to prevent duplicates)
    private HashSet<string> seenRelics = new();

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
        var uiScene = ResourceLoader.Load<PackedScene>("res://Scenes/UI/PersistentUI.tscn");
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

    public void AddGold(int amount)
    {
        PlayerGold += amount;
        // Clamp gold to minimum of 0 (can't go negative)
        PlayerGold = Mathf.Max(0, PlayerGold);
        UpdateUI();
    }

    // Inventory management
    public bool AddItem(string itemId)
    {
        // Maximum 3 items allowed in inventory
        if (items.Count >= 3)
        {
            GD.Print($"Inventory full! Cannot add {itemId}");
            return false;
        }

        items.Add(itemId);
        UpdateUI();
        return true;
    }

    public void RemoveItem(string itemId)
    {
        items.Remove(itemId);
        UpdateUI();
    }

    public void AddRelic(string relicId)
    {
        relics.Add(relicId);
        MarkRelicAsSeen(relicId);
        UpdateUI();
    }

    // Relic tracking methods
    public void MarkRelicAsSeen(string relicId)
    {
        seenRelics.Add(relicId);
    }

    public bool HasSeenRelic(string relicId)
    {
        return seenRelics.Contains(relicId);
    }

    public HashSet<string> GetSeenRelics()
    {
        return new HashSet<string>(seenRelics);
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
        PlayerHealth = StartingHealth;
        MaxPlayerHealth = StartingMaxHealth;
        PlayerGold = StartingGold;
        items.Clear();
        relics.Clear();
        seenRelics.Clear();
        temporaryBuffs.Clear();
        permanentBuffs.Clear();
        LastKillerName = "";
        LastScore = DefaultScore;
        UpdateUI();
    }
}
