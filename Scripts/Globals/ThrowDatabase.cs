using Godot;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using Rps;

public partial class ThrowDatabase : Node
{
    public static ThrowDatabase Instance { get; private set; }

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
            Attributes = new List<ThrowAttribute>(template.Attributes),
            Evolved = template.Evolved,
            Effect = new ThrowEffectData
            {
                EffectType = template.Effect.EffectType,
                ThrowType = template.Effect.ThrowType,
                BaseDamage = template.Effect.BaseDamage,
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
}

// Helper class for JSON deserialization
public class ThrowDatabaseJson
{
    public Dictionary<string, ThrowData> Throws { get; set; }
}
