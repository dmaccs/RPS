using Godot;
using System.Collections.Generic;
using System.Text.Json;

public partial class BattleProgressionManager : Node
{
    public static BattleProgressionManager Instance { get; private set; }

    private BattleProgressionData progressionData;
    private int currentStage = 0; // Stage number, increments before each battle

    public override void _Ready()
    {
        Instance = this;
        LoadBattleProgression();
    }

    private void LoadBattleProgression()
    {
        string path = "res://Data/BattleProgression.json";

        if (!FileAccess.FileExists(path))
        {
            GD.PrintErr($"BattleProgression.json not found at {path}");
            return;
        }

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        string jsonText = file.GetAsText();

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            progressionData = JsonSerializer.Deserialize<BattleProgressionData>(jsonText, options);
            GD.Print($"Loaded {progressionData.Battles.Count} battle configurations");
        }
        catch (System.Exception e)
        {
            GD.PrintErr($"Failed to parse BattleProgression.json: {e.Message}");
        }
    }

    public void IncrementStage()
    {
        currentStage++;
        GD.Print($"Stage incremented to: {currentStage} (Group {GetCurrentGroup()})");
    }

    public void ResetBattles()
    {
        currentStage = 0;
        GD.Print("Battle progression reset");
    }

    public int GetCurrentStage()
    {
        return currentStage;
    }

    public int GetCurrentGroup()
    {
        // Calculate group as ceiling of stage / 3
        // Stages 1-3 = Group 1, 4-6 = Group 2, etc.
        // Clamp to max group 5
        if (currentStage == 0) return 1;
        int group = (int)Godot.Mathf.Ceil(currentStage / 3.0f);
        return Godot.Mathf.Min(group, 5);
    }

    public string GetEnemyForCurrentBattle()
    {
        if (progressionData == null || progressionData.Battles == null || progressionData.Battles.Count == 0)
        {
            GD.PrintErr("No battle progression data loaded, using fallback");
            return "RandomEnemy";
        }

        int currentGroup = GetCurrentGroup();

        // Find the battle configuration for current group
        BattleConfig config = null;

        for (int i = 0; i < progressionData.Battles.Count; i++)
        {
            if (progressionData.Battles[i].Group == currentGroup)
            {
                config = progressionData.Battles[i];
                break;
            }
        }

        // If no exact match, use the last available battle config
        if (config == null)
        {
            config = progressionData.Battles[progressionData.Battles.Count - 1];
            GD.Print($"Group {currentGroup} not defined, using group {config.Group} pool");
        }

        // Select enemy from pool using weights
        return SelectWeightedEnemy(config, currentGroup);
    }

    private string SelectWeightedEnemy(BattleConfig config, int group)
    {
        if (config.EnemyPool == null || config.EnemyPool.Count == 0)
        {
            GD.PrintErr("Empty enemy pool in battle config");
            return "RandomEnemy";
        }

        // If no weights specified, use uniform distribution
        if (config.Weights == null || config.Weights.Count != config.EnemyPool.Count)
        {
            int index = RngManager.Instance.Rng.RandiRange(0, config.EnemyPool.Count - 1);
            return config.EnemyPool[index];
        }

        // Weighted selection
        int totalWeight = 0;
        foreach (int weight in config.Weights)
        {
            totalWeight += weight;
        }

        int randomValue = RngManager.Instance.Rng.RandiRange(1, totalWeight);
        int cumulative = 0;

        for (int i = 0; i < config.EnemyPool.Count; i++)
        {
            cumulative += config.Weights[i];
            if (randomValue <= cumulative)
            {
                GD.Print($"Selected {config.EnemyPool[i]} for stage {currentStage} (group {group})");
                return config.EnemyPool[i];
            }
        }

        // Fallback (should never reach here)
        return config.EnemyPool[0];
    }
}

// Data classes for JSON deserialization
public class BattleProgressionData
{
    public List<BattleConfig> Battles { get; set; }
}

public class BattleConfig
{
    public int Group { get; set; }
    public List<string> EnemyPool { get; set; }
    public List<int> Weights { get; set; }
}