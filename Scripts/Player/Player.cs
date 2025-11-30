using System.Collections.Generic;
using System.Linq;
using Godot;
using Rps;

public partial class Player : Node
{
    // === NEW THROW SYSTEM ===
    // Equipped throws (active in battle) - 3 slots, null = empty
    public ThrowData[] EquippedThrows { get; private set; } = new ThrowData[3];

    // Inventory throws (unequipped storage) - 3 slots, null = empty
    public ThrowData[] InventoryThrows { get; private set; } = new ThrowData[3];

    // === DEPRECATED: Keep for backward compatibility during migration ===
    public List<MoveData> CurrentThrows { get; private set; } = new List<MoveData>
    {
        new MoveData(Throws.rock, 1),
        new MoveData(Throws.paper, 1),
        new MoveData(Throws.scissors, 1)
    };

    // Expose current stats by delegating to GameState
    public int Health => GameState.Instance.PlayerHealth;

    // Apply damage/heal through GameState so the value is persistent
    public void Damage(int amount)
    {
        GameState.Instance.ModifyHealth(-amount);
    }

    public void Heal(int amount)
    {
        GameState.Instance.ModifyHealth(amount);
    }

    public bool IsDead() => GameState.Instance.PlayerHealth <= 0;

    public void AddGold(int amount)
    {
        GameState.Instance.AddGold(amount);
    }

    // Track player's last throw (previous round). Null if none yet.
    public Throws? LastThrow { get; set; } = null;

    // History of throws for this run (does not include the current throw until after ResolveRound)
    public List<Throws> ThrowHistory { get; private set; } = new List<Throws>();

    // === NEW THROW SYSTEM METHODS ===

    // Get count of non-null equipped throws
    public int GetEquippedCount()
    {
        return EquippedThrows.Count(t => t != null);
    }

    // Get count of non-null inventory throws
    public int GetInventoryCount()
    {
        return InventoryThrows.Count(t => t != null);
    }

    // Get all non-null equipped throws (for battle system)
    public List<ThrowData> GetEquippedThrowsList()
    {
        return EquippedThrows.Where(t => t != null).ToList();
    }

    // Initialize throws with starting set (call on new game)
    public void InitializeThrows()
    {
        // Clear all slots
        for (int i = 0; i < 3; i++)
        {
            EquippedThrows[i] = null;
            InventoryThrows[i] = null;
        }

        if (ThrowDatabase.Instance == null)
        {
            GD.PrintErr("ThrowDatabase not initialized!");
            return;
        }

        var startingIds = ThrowDatabase.Instance.GetStartingThrowIds();
        for (int i = 0; i < startingIds.Count && i < 3; i++)
        {
            var throwData = ThrowDatabase.Instance.CreateInstance(startingIds[i]);
            if (throwData != null)
            {
                EquippedThrows[i] = throwData;
            }
        }

        GD.Print($"Initialized {GetEquippedCount()} starting throws");
        GameState.Instance?.RefreshUI();
    }

    // Find first empty slot in array, returns -1 if full
    private int FindEmptySlot(ThrowData[] slots)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) return i;
        }
        return -1;
    }

    // Add throw to inventory by ThrowData
    public bool AddThrowToInventory(ThrowData throwData)
    {
        int slot = FindEmptySlot(InventoryThrows);
        if (slot == -1)
        {
            GD.Print("Inventory full - cannot add throw");
            return false;
        }

        InventoryThrows[slot] = throwData;
        GameState.Instance?.RefreshUI();
        return true;
    }

    // Add throw to inventory by ID
    public bool AddThrowToInventory(string throwId)
    {
        var throwData = ThrowDatabase.Instance?.CreateInstance(throwId);
        if (throwData == null)
        {
            GD.PrintErr($"Failed to create throw: {throwId}");
            return false;
        }
        return AddThrowToInventory(throwData);
    }

    // Add throw directly to equipped slots (if space available)
    public bool AddThrowToEquipped(ThrowData throwData)
    {
        int slot = FindEmptySlot(EquippedThrows);
        if (slot == -1)
        {
            GD.Print("Equipped slots full - cannot add throw");
            return false;
        }

        EquippedThrows[slot] = throwData;
        GameState.Instance?.RefreshUI();
        return true;
    }

    // Add throw directly to equipped slots by ID
    public bool AddThrowToEquipped(string throwId)
    {
        var throwData = ThrowDatabase.Instance?.CreateInstance(throwId);
        if (throwData == null)
        {
            GD.PrintErr($"Failed to create throw: {throwId}");
            return false;
        }
        return AddThrowToEquipped(throwData);
    }

    // Set throw at specific equipped slot
    public void SetEquippedSlot(int slotIndex, ThrowData throwData)
    {
        if (slotIndex >= 0 && slotIndex < 3)
        {
            EquippedThrows[slotIndex] = throwData;
            GameState.Instance?.RefreshUI();
        }
    }

    // Set throw at specific inventory slot
    public void SetInventorySlot(int slotIndex, ThrowData throwData)
    {
        if (slotIndex >= 0 && slotIndex < 3)
        {
            InventoryThrows[slotIndex] = throwData;
            GameState.Instance?.RefreshUI();
        }
    }

    // Discard throw from inventory slot
    public bool DiscardThrow(int inventoryIndex)
    {
        if (inventoryIndex < 0 || inventoryIndex >= 3 || InventoryThrows[inventoryIndex] == null)
        {
            GD.Print("Invalid inventory index or empty slot");
            return false;
        }

        InventoryThrows[inventoryIndex] = null;
        GameState.Instance?.RefreshUI();
        return true;
    }

    // Get total count of a specific attribute across equipped throws
    public int GetEquippedAttributeCount(ThrowAttribute attribute)
    {
        int count = 0;
        foreach (var t in EquippedThrows)
        {
            if (t != null)
                count += t.Attributes.Count(a => a == attribute);
        }
        return count;
    }

    // Get total count of a specific attribute across all throws (equipped + inventory)
    public int GetTotalAttributeCount(ThrowAttribute attribute)
    {
        int count = GetEquippedAttributeCount(attribute);
        foreach (var t in InventoryThrows)
        {
            if (t != null)
                count += t.Attributes.Count(a => a == attribute);
        }
        return count;
    }

    // Check if at full capacity (6 total throws)
    public bool IsThrowCapacityFull()
    {
        return (GetEquippedCount() + GetInventoryCount()) >= 6;
    }

    // Upgrade a specific equipped throw's base damage
    public bool UpgradeEquippedThrow(int equippedIndex)
    {
        if (equippedIndex < 0 || equippedIndex >= 3 || EquippedThrows[equippedIndex] == null)
        {
            GD.Print("Invalid equipped index or empty slot");
            return false;
        }

        EquippedThrows[equippedIndex].Effect.BaseDamage++;
        GameState.Instance?.RefreshUI();
        return true;
    }

    // === DEPRECATED METHODS (keep for backward compatibility) ===

    public void AddMove(Throws type, int level = 1)
    {
        if (CurrentThrows.Count >= 3)
        {
            GD.Print("Cannot have more than 3 moves.");
            return;
        }

        CurrentThrows.Add(new MoveData(type, level));
        GameState.Instance?.RefreshUI();
    }

    public void UpgradeThrow(Throws type)
    {
        var move = CurrentThrows.Find(m => m.Type == type);
        if (move != null)
        {
            move.Level++;
            GameState.Instance?.RefreshUI();
        }
        else
        {
            GD.Print("Move not found.");
        }
    }

    public void RemoveMove(Throws type)
    {
        CurrentThrows.RemoveAll(m => m.Type == type);
        GameState.Instance?.RefreshUI();
    }
}
