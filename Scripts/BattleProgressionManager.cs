using Godot;
using System.Collections.Generic;
using System.Text.Json;

public partial class BattleProgressionManager : Node
{
    public static BattleProgressionManager Instance { get; private set; }

    private BattleProgressionData progressionData;
    private int currentBattleNumber = 0; // 0-indexed, increments before each battle

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

    public void IncrementBattle()
    {
        currentBattleNumber++;
        GD.Print($"Battle number incremented to: {currentBattleNumber}");
    }

    public void ResetBattles()
    {
        currentBattleNumber = 0;
        GD.Print("Battle progression reset");
    }

    public string GetEnemyForCurrentBattle()
    {
        if (progressionData == null || progressionData.Battles == null || progressionData.Battles.Count == 0)
        {
            GD.PrintErr("No battle progression data loaded, using fallback");
            return "RandomEnemy";
        }

        // Find the battle configuration for current battle number
        // If battleNumber exceeds defined battles, use the last battle's pool
        BattleConfig config = null;

        for (int i = 0; i < progressionData.Battles.Count; i++)
        {
            if (progressionData.Battles[i].BattleNumber == currentBattleNumber)
            {
                config = progressionData.Battles[i];
                break;
            }
        }

        // If no exact match, use the last available battle config
        if (config == null)
        {
            config = progressionData.Battles[progressionData.Battles.Count - 1];
            GD.Print($"Battle {currentBattleNumber} not defined, using battle {config.BattleNumber} pool");
        }

        // Select enemy from pool using weights
        return SelectWeightedEnemy(config);
    }

    private string SelectWeightedEnemy(BattleConfig config)
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
                GD.Print($"Selected {config.EnemyPool[i]} for battle {currentBattleNumber}");
                return config.EnemyPool[i];
            }
        }

        // Fallback (should never reach here)
        return config.EnemyPool[0];
    }

    public int GetCurrentBattleNumber()
    {
        return currentBattleNumber;
    }
}

// Data classes for JSON deserialization
public class BattleProgressionData
{
    public List<BattleConfig> Battles { get; set; }
}

public class BattleConfig
{
    public int BattleNumber { get; set; }
    public List<string> EnemyPool { get; set; }
    public List<int> Weights { get; set; }
}