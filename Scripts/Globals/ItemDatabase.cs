using Godot;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;

public enum ItemCategory
{
    Consumable,
    Relic,
    Unknown
}

public partial class ItemDatabase : Node
{
    public static ItemDatabase Instance { get; private set; }

    private Dictionary<string, ConsumableItemData> consumables = new();
    private Dictionary<string, RelicData> relics = new();

    public override void _Ready()
    {
        Instance = this;
        LoadData();
    }

    private void LoadData()
    {
        string path = "res://Data/ItemData.json";

        // Ensure file exists
        if (!FileAccess.FileExists(path))
        {
            GD.PrintErr($"Item JSON not found at {path}");
            return;
        }

        // Read the file
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        string json = file.GetAsText();

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            var data = JsonSerializer.Deserialize<ItemDatabaseJson>(json, options);

            if (data != null)
            {
                consumables = data.Consumables ?? new();
                relics = data.Relics ?? new();

                GD.Print($"Loaded {consumables.Count} consumables and {relics.Count} relics from {path}");
            }
        }
        catch (System.Exception e)
        {
            GD.PrintErr($"Failed to parse {path}: {e.Message}");
        }
    }

    public ItemCategory GetItemCategory(string id)
    {
        if (consumables.ContainsKey(id))
            return ItemCategory.Consumable;

        if (relics.ContainsKey(id))
            return ItemCategory.Relic;

        return ItemCategory.Unknown;
    }

    public ConsumableItemData GetConsumable(string id)
    {
        if (consumables.TryGetValue(id, out var item))
            return item;

        GD.PrintErr($"Consumable not found: {id}");
        return null;
    }

    public RelicData GetRelic(string id)
    {
        if (relics.TryGetValue(id, out var relic))
            return relic;

        GD.PrintErr($"Relic not found: {id}");
        return null;
    }

    public List<ItemData> GetAllItems()
    {
        var allItems = new List<ItemData>();
        allItems.AddRange(consumables.Values);
        allItems.AddRange(relics.Values);
        return allItems;
    }

    public List<ConsumableItemData> GetAllConsumables()
    {
        return consumables.Values.ToList();
    }

    public List<RelicData> GetAllRelics()
    {
        return relics.Values.ToList();
    }

    public List<ItemData> GetItemsByRarity(ItemRarity rarity)
    {
        return GetAllItems().Where(item => item.Rarity == rarity).ToList();
    }

    public List<ConsumableItemData> GetConsumablesByRarity(ItemRarity rarity)
    {
        return consumables.Values.Where(item => item.Rarity == rarity).ToList();
    }

    public List<RelicData> GetRelicsByRarity(ItemRarity rarity)
    {
        return relics.Values.Where(item => item.Rarity == rarity).ToList();
    }

    // Get relics by rarity that haven't been seen yet this run
    public List<RelicData> GetUnseenRelicsByRarity(ItemRarity rarity)
    {
        var seenRelics = GameState.Instance.GetSeenRelics();
        return relics.Values
            .Where(relic => relic.Rarity == rarity && !seenRelics.Contains(relic.Id))
            .ToList();
    }

    // Get all unseen relics
    public List<RelicData> GetUnseenRelics()
    {
        var seenRelics = GameState.Instance.GetSeenRelics();
        return relics.Values
            .Where(relic => !seenRelics.Contains(relic.Id))
            .ToList();
    }
}

// Helper class for JSON deserialization
public class ItemDatabaseJson
{
    public Dictionary<string, ConsumableItemData> Consumables { get; set; }
    public Dictionary<string, RelicData> Relics { get; set; }
}
