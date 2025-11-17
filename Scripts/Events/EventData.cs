using Godot;
using System;
using System.Collections.Generic;

public enum EventRarity
{
    Common,
    Uncommon,
    Rare
}

// Data class for event outcomes
public class EventOutcomeData
{
    public string Type { get; set; }
    public int Amount { get; set; }
    public string TargetMove { get; set; } // For move-specific upgrades (rock/paper/scissors)
    public int SuccessRate { get; set; } // For random outcomes (0-100)
    public List<EventOutcomeData> SuccessOutcomes { get; set; } // For random outcomes
    public List<EventOutcomeData> FailureOutcomes { get; set; } // For random outcomes
    public string ItemId { get; set; } // For add_item/add_relic outcomes
}

// Data class for event options
public class EventOptionData
{
    public string Text { get; set; }
    public List<EventOutcomeData> Outcomes { get; set; }
}

// Data class for events
public class EventData
{
    public string Id { get; set; }
    public int Stage { get; set; } // Which stage this event can appear in
    public string Title { get; set; }
    public string Description { get; set; }
    public EventRarity Rarity { get; set; }
    public string ColorHex { get; set; } // Hex color for placeholder panel (e.g., "#808080")
    public List<EventOptionData> Options { get; set; }
}
