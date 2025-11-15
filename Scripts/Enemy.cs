using Godot;
using Rps;
using System;

public partial class Enemy : Node2D
{
    public int strength = 1;
    private int health = 3;
    public string id;
    private EnemyData enemyData;
    private RandomNumberGenerator rng;
    private IEnemyBehavior behaviorInstance;
    public bool isBoss = false;

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
            lbl.Text = id;
        }

        GD.Print($"Enemy initialized: {id} (display name: {id})");
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

    // previousPlayerThrow: the player's last throw (from previous round), may be null
    // playerHistory: list of previous player throws in this run (does not include current throw)
    public Throws ChooseThrow(Throws? previousPlayerThrow = null, System.Collections.Generic.List<Throws> playerHistory = null)
    {
        if (behaviorInstance != null)
            return behaviorInstance.ChooseThrow(previousPlayerThrow, playerHistory);

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
    
    public void OnBattleResult()
    {
        GD.Print("Battle over");//TODO: make this actually do things
    }
}
