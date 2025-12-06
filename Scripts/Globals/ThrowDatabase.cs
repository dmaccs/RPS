using Godot;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using Rps;

public partial class ThrowDatabase : Node
{
    public static ThrowDatabase Instance { get; private set; }

    // Rarity weights for reward selection (cumulative percentages)
    public static int CommonWeight = 60;      // 60% chance
    public static int UncommonWeight = 37;    // 37% chance
    public static int RareWeight = 3;         // 3% chance

    private Dictionary<string, ThrowData> throws = new();

    public override void _Ready()
    {
        Instance = this;
        LoadData();
    }

    private void LoadData()
    {
        string path = "res://Data/ThrowData.json";

        if (!FileAccess.FileExists(path))
        {
            GD.PrintErr($"Throw JSON not found at {path}");
            return;
        }

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        string json = file.GetAsText();

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            var data = JsonSerializer.Deserialize<ThrowDatabaseJson>(json, options);

            if (data != null)
            {
                throws = data.Throws ?? new();
                GD.Print($"Loaded {throws.Count} throws from {path}");
            }
        }
        catch (System.Exception e)
        {
            GD.PrintErr($"Failed to parse {path}: {e.Message}");
        }
    }

    // Get throw data by ID (returns the template - do not modify)
    public ThrowData Get(string id)
    {
        if (throws.TryGetValue(id, out var throwData))
            return throwData;

        GD.PrintErr($"Throw not found: {id}");
        return null;
    }

    // Create a new instance of a throw (safe to modify)
    public ThrowData CreateInstance(string id)
    {
        var template = Get(id);
        if (template == null) return null;

        // Create a deep copy so modifications don't affect the template
        return new ThrowData
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            Attributes = new List<ThrowAttribute>(template.Attributes),
            Evolved = template.Evolved,
            Rarity = template.Rarity,
            Cost = template.Cost,
            Effect = new ThrowEffectData
            {
                EffectType = template.Effect.EffectType,
                ThrowType = template.Effect.ThrowType,
                BaseDamage = template.Effect.BaseDamage,
                BaseStats = template.Effect.BaseStats != null
                    ? new ThrowStats
                    {
                        Damage = template.Effect.BaseStats.Damage,
                        Block = template.Effect.BaseStats.Block,
                        Heal = template.Effect.BaseStats.Heal,
                        Lifesteal = template.Effect.BaseStats.Lifesteal
                    }
                    : null,
                OutcomeOverrides = template.Effect.OutcomeOverrides != null
                    ? new Dictionary<string, ThrowStats>(template.Effect.OutcomeOverrides)
                    : null,
                Parameters = template.Effect.Parameters != null
                    ? new Dictionary<string, object>(template.Effect.Parameters)
                    : new Dictionary<string, object>()
            }
        };
    }

    // Get all available throws
    public List<ThrowData> GetAllThrows()
    {
        return throws.Values.ToList();
    }

    // Get throws that have a specific attribute
    public List<ThrowData> GetThrowsByAttribute(ThrowAttribute attr)
    {
        return throws.Values.Where(t => t.Attributes.Contains(attr)).ToList();
    }

    // Get throws by evolution state
    public List<ThrowData> GetThrowsByEvolved(bool evolved)
    {
        return throws.Values.Where(t => t.Evolved == evolved).ToList();
    }

    // Get the starting throw IDs for a new game
    public List<string> GetStartingThrowIds()
    {
        return new List<string> { "basic_rock", "basic_paper", "basic_scissors" };
    }

    // Check if a throw ID exists
    public bool Exists(string id)
    {
        return throws.ContainsKey(id);
    }

    // Get throws by rarity
    public List<ThrowData> GetThrowsByRarity(ThrowRarity rarity)
    {
        return throws.Values.Where(t => t.Rarity == rarity).ToList();
    }

    // Get all non-starter throws (for rewards/shop)
    public List<ThrowData> GetRewardableThrows()
    {
        return throws.Values.Where(t => t.Rarity != ThrowRarity.Starter).ToList();
    }

    // Select random throws weighted by rarity
    public List<ThrowData> SelectRandomThrowsByRarity(int count, RandomNumberGenerator rng = null)
    {
        rng ??= new RandomNumberGenerator();
        var selected = new List<ThrowData>();
        var available = GetRewardableThrows();

        int totalWeight = CommonWeight + UncommonWeight + RareWeight;

        for (int i = 0; i < count && available.Count > 0; i++)
        {
            int roll = rng.RandiRange(1, totalWeight);
            ThrowRarity targetRarity;

            if (roll <= CommonWeight)
                targetRarity = ThrowRarity.Common;
            else if (roll <= CommonWeight + UncommonWeight)
                targetRarity = ThrowRarity.Uncommon;
            else
                targetRarity = ThrowRarity.Rare;

            // Get throws of that rarity from available pool
            var rarityPool = available.Where(t => t.Rarity == targetRarity).ToList();

            // If no throws of that rarity available, fall back to any available
            if (rarityPool.Count == 0)
                rarityPool = available;

            if (rarityPool.Count > 0)
            {
                int index = rng.RandiRange(0, rarityPool.Count - 1);
                var selectedThrow = rarityPool[index];
                selected.Add(selectedThrow);
                available.Remove(selectedThrow);
            }
        }

        return selected;
    }
}

// Helper class for JSON deserialization
public class ThrowDatabaseJson
{
    public Dictionary<string, ThrowData> Throws { get; set; }
}
