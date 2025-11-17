using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

public partial class EventDatabase : Node
{
    public static EventDatabase Instance { get; private set; }

    private Dictionary<string, EventData> events = new();

    public override void _Ready()
    {
        Instance = this;
        LoadEvents();
    }

    private void LoadEvents()
    {
        var file = FileAccess.Open("res://Data/EventData.json", FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr("Failed to load EventData.json");
            return;
        }

        string jsonText = file.GetAsText();
        file.Close();

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };

            var eventDict = JsonSerializer.Deserialize<Dictionary<string, EventData>>(jsonText, options);
            if (eventDict != null)
            {
                events = eventDict;
                GD.Print($"Loaded {events.Count} events from EventData.json");
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error parsing EventData.json: {ex.Message}");
        }
    }

    public EventData GetEvent(string id)
    {
        return events.TryGetValue(id, out var eventData) ? eventData : null;
    }

    public List<EventData> GetEventsByStage(int stage)
    {
        return events.Values.Where(e => e.Stage == stage).ToList();
    }

    public List<EventData> GetEventsByStageAndRarity(int stage, EventRarity rarity)
    {
        return events.Values.Where(e => e.Stage == stage && e.Rarity == rarity).ToList();
    }

    public EventData SelectRandomEvent(int stage)
    {
        var stageEvents = GetEventsByStage(stage);

        if (stageEvents.Count == 0)
        {
            GD.PrintErr($"No events found for stage {stage}");
            return null;
        }

        var rng = RngManager.Instance?.Rng ?? new RandomNumberGenerator();

        // Rarity weights: Common = 60%, Uncommon = 30%, Rare = 10%
        var rarityWeights = new Dictionary<EventRarity, float>
        {
            { EventRarity.Common, 0.6f },
            { EventRarity.Uncommon, 0.3f },
            { EventRarity.Rare, 0.1f }
        };

        // Calculate total weight
        float totalWeight = 0f;
        foreach (var evt in stageEvents)
        {
            totalWeight += rarityWeights[evt.Rarity];
        }

        // Select random event based on weight
        float randomValue = rng.Randf() * totalWeight;
        float cumulative = 0f;

        foreach (var evt in stageEvents)
        {
            cumulative += rarityWeights[evt.Rarity];
            if (randomValue <= cumulative)
            {
                GD.Print($"Selected event: {evt.Title} (Rarity: {evt.Rarity})");
                return evt;
            }
        }

        // Fallback to first event if something goes wrong
        return stageEvents[0];
    }

    public List<EventData> GetAllEvents()
    {
        return events.Values.ToList();
    }
}
