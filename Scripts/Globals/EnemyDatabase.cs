using Godot;
using System.Collections.Generic;
using System.Text.Json;

public partial class EnemyDatabase : Node
{

    public static EnemyDatabase Instance { get; private set; }
    private Dictionary<string, EnemyData> enemies = [];

    public override void _Ready()
    {
        Instance = this;
        LoadData();
    }

    private void LoadData()
    {
        string path = "res://Data/EnemyData.json";

        // Ensure file exists
        if (!FileAccess.FileExists(path)){
            GD.PrintErr($"Enemy JSON not found at {path}");
            return;
        }

        // Read the file
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        string json = file.GetAsText();

		try{
			var options = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			};

			enemies = JsonSerializer.Deserialize<Dictionary<string, EnemyData>>(json, options) ?? [];

			GD.Print($"Loaded {enemies.Count} enemies from {path}");
		} catch (System.Exception e)
		{
			GD.PrintErr($"Failed to parse {path}: {e.Message}");
		}
    }

    public EnemyData Get(string id)
    {
        if (enemies.TryGetValue(id, out var enemy))
            return enemy;

        GD.PrintErr($"Enemy not found: {id}");
        return null;
    }
}
