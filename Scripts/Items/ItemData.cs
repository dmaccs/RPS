using Godot;
using System;
using System.Collections.Generic;

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare
}

// Base class for all purchasable items
public abstract class ItemData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int Cost { get; set; }
    public ItemRarity Rarity { get; set; }
}

// Consumable items that are used once and removed from inventory
public class ConsumableItemData : ItemData
{
    public ItemEffectData Effect { get; set; }
}

// Permanent relics that provide passive or ongoing effects
public class RelicData : ItemData
{
    public ItemEffectData Effect { get; set; }
}

public class ItemEffectData
{
    public string EffectType { get; set; }
    public int Amount { get; set; }
    public int Duration { get; set; } // For temporary buffs (0 = permanent/instant)
    public Dictionary<string, object> AdditionalData { get; set; } // For complex effects
}
