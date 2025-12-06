using Godot;
using Rps;
using System;
using System.Collections.Generic;

public partial class Enemy : Node2D
{
    public int strength = 1;
    private int health = 3;
    public string id;
    private EnemyData enemyData;
    private RandomNumberGenerator rng;
    private IEnemyBehavior behaviorInstance;
    public bool isBoss = false;
    public string BehaviorName => enemyData?.behavior;

    // Status effects tracking (e.g., radioactive, poison, stun)
    private Dictionary<string, int> statusEffects = new Dictionary<string, int>();

    private Label nameLabel;

    private Label healthLabel;

    private Label strengthLabel;

    [Signal]
    public delegate void FightEndSignalEventHandler(bool result);

    // Parameterless constructor used by Godot when instancing the PackedScene
    public Enemy()
    {
    }

    // Initialize enemy data after construction/instancing
    public void Initialize(string id)
    {
        this.id = id;
        GD.Print("Initializing enemy: " + id);
        enemyData = EnemyDatabase.Instance.Get(id);
        if (enemyData != null)
        {
            this.health = enemyData.health;
            this.strength = enemyData.strength;
            this.isBoss = enemyData.isBoss;
        }
        rng = RngManager.Instance?.Rng ?? new RandomNumberGenerator();

        // Create behavior instance from data
        behaviorInstance = BehaviorFactory.Create(enemyData?.behavior, enemyData, rng);

        healthLabel = GetNodeOrNull<Label>("Health");
        strengthLabel = GetNodeOrNull<Label>("Strength");
        Label lbl = GetNodeOrNull<Label>("Name");
        if(healthLabel != null){
            healthLabel.Text = "Health: " + health.ToString();
        }
        if(strengthLabel != null){
            strengthLabel.Text = "Strength: " + strength.ToString();
        }
        if (lbl != null)
        {
            lbl.Text = enemyData?.displayName ?? id;
        }

        GD.Print($"Enemy initialized: {id} (display name: {enemyData?.displayName ?? id})");
    }

    public void TakeDamage(int dmg)
    {
        health = health - dmg;
        RefreshHealthLabel();
        if (health <= 0)
        {
            EmitSignal(SignalName.FightEndSignal, true);
        }
    }

    public void RefreshHealthLabel()
    {
        if(healthLabel != null){
            healthLabel.Text = "Health: " + health.ToString();
        }
    }

    public void RefreshStrengthLabel()
    {
        if(strengthLabel != null){
            strengthLabel.Text = "Strength: " + strength.ToString();
        }
    }

    public void OnDraw()
    {
        // Check if this enemy gains strength on draws
        if (enemyData?.gainsStrengthOnDraw ?? false)
        {
            strength += 1;
            RefreshStrengthLabel();
            GD.Print($"{id} gained strength from draw! New strength: {strength}");
        }
    }

    // previousPlayerThrow: the player's last throw (from previous round), may be null
    // playerHistory: list of previous player throws in this run (does not include current throw)
    // encounterPlayerThrows: list of lists - each round's player throws in this encounter
    // encounterEnemyThrows: list of lists - each round's enemy throws in this encounter
    public Throws ChooseThrow(Throws? previousPlayerThrow = null, System.Collections.Generic.List<Throws> playerHistory = null,
                              System.Collections.Generic.List<System.Collections.Generic.List<Throws>> encounterPlayerThrows = null,
                              System.Collections.Generic.List<System.Collections.Generic.List<Throws>> encounterEnemyThrows = null)
    {
        if (behaviorInstance != null)
            return behaviorInstance.ChooseThrow(previousPlayerThrow, playerHistory, encounterPlayerThrows, encounterEnemyThrows);

        // fallback: rock
        return Throws.rock;
    }

    private bool Beats(Throws a, Throws b)
    {
        return (a == Throws.rock && b == Throws.scissors)
            || (a == Throws.paper && b == Throws.rock)
            || (a == Throws.scissors && b == Throws.paper);
    }

    private Throws WinningThrowAgainst(Throws t)
    {
        if (t == Throws.rock) return Throws.paper;
        if (t == Throws.paper) return Throws.scissors;
        if (t == Throws.scissors) return Throws.rock;
        // default
        return Throws.rock;
    }

    public string GetDisplayName()
    {
        return enemyData?.displayName ?? id ?? "Unknown Enemy";
    }

    public void OnBattleResult()
    {
        GD.Print("Battle over");//TODO: make this actually do things
    }

    // Status effect methods
    public void ApplyStatusEffect(string type, int stacks)
    {
        if (statusEffects.ContainsKey(type))
            statusEffects[type] += stacks;
        else
            statusEffects[type] = stacks;

        GD.Print($"{GetDisplayName()} gained {stacks} stack(s) of {type}. Total: {statusEffects[type]}");
    }

    public int GetStatusStacks(string type)
    {
        return statusEffects.TryGetValue(type, out var stacks) ? stacks : 0;
    }

    public void RemoveStatusEffect(string type)
    {
        if (statusEffects.ContainsKey(type))
        {
            statusEffects.Remove(type);
            GD.Print($"{GetDisplayName()} lost all stacks of {type}");
        }
    }

    public void ClearAllStatusEffects()
    {
        statusEffects.Clear();
        GD.Print($"{GetDisplayName()} cleared all status effects");
    }

    // Process status effects at start of round (returns damage dealt)
    public int ProcessStatusEffects()
    {
        int totalDamage = 0;

        // Radioactive: deal damage equal to stacks
        if (statusEffects.TryGetValue("radioactive", out int radioactiveStacks) && radioactiveStacks > 0)
        {
            totalDamage += radioactiveStacks;
            GD.Print($"{GetDisplayName()} takes {radioactiveStacks} radioactive damage");
        }

        // Poison: deal damage equal to stacks, then reduce by 1
        if (statusEffects.TryGetValue("poison", out int poisonStacks) && poisonStacks > 0)
        {
            totalDamage += poisonStacks;
            statusEffects["poison"] = poisonStacks - 1;
            if (statusEffects["poison"] <= 0)
                statusEffects.Remove("poison");
            GD.Print($"{GetDisplayName()} takes {poisonStacks} poison damage");
        }

        if (totalDamage > 0)
        {
            TakeDamage(totalDamage);
        }

        return totalDamage;
    }

    public Dictionary<string, int> GetAllStatusEffects()
    {
        return new Dictionary<string, int>(statusEffects);
    }
}
