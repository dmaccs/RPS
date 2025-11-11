using System.Collections.Generic;
using Godot;
using Rps;

public partial class Player : Node
{
    // Moves are part of the active gameplay state (not the global save/state)
    public List<MoveData> CurrentThrows { get; private set; } = new List<MoveData>
    {
        new MoveData(Throws.rock, 1),
        new MoveData(Throws.paper, 1),
        new MoveData(Throws.scissors, 1)
    };

    // Expose current stats by delegating to GameState
    public int Health => GameState.Instance.PlayerHealth;

    // Apply damage/heal through GameState so the value is persistent
    public void Damage(int amount)
    {
        GameState.Instance.ModifyHealth(-amount);
    }

    public void Heal(int amount)
    {
        GameState.Instance.ModifyHealth(amount);
    }

    // Player move strength is stored per-move in CurrentThrows (MoveData.Level).
    // Old global player strength has been removed.

    public bool IsDead() => GameState.Instance.PlayerHealth <= 0;

    public void AddGold(int amount)
    {
        GameState.Instance.AddGold(amount);
    }

    // Track player's last throw (previous round). Null if none yet.
    public Throws? LastThrow { get; set; } = null;

    // History of throws for this run (does not include the current throw until after ResolveRound)
    public System.Collections.Generic.List<Throws> ThrowHistory { get; private set; } = new System.Collections.Generic.List<Throws>();

    // Move management (gameplay rules live here)
    public void AddMove(Throws type, int level = 1)
    {
        if (CurrentThrows.Count >= 3)
        {
            GD.Print("Cannot have more than 3 moves.");
            return;
        }

        CurrentThrows.Add(new MoveData(type, level));
        // Refresh persistent UI so new move shows up immediately
        GameState.Instance?.RefreshUI();
    }

    public void UpgradeThrow(Throws type)
    {
        var move = CurrentThrows.Find(m => m.Type == type);
        if (move != null)
        {
            move.Level++;
            // Refresh persistent UI to reflect the new level
            GameState.Instance?.RefreshUI();
        }
        else
        {
            GD.Print("Move not found.");
        }
    }

    public void RemoveMove(Throws type)
    {
        CurrentThrows.RemoveAll(m => m.Type == type);
        GameState.Instance?.RefreshUI();
    }
}
