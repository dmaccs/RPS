using System.Collections.Generic;
using Godot;
using Rps;
using RPS.Scripts.Behaviors;

public enum RoundOutcome
{
    PlayerWin,
    EnemyWin,
    Draw
}

public class RoundResult
{
    public Throws PlayerThrow { get; set; }
    public Throws EnemyThrow { get; set; }
    public RoundOutcome Outcome { get; set; }
    public int DamageDealt { get; set; }
    public string DamageTarget { get; set; } // "player" or "enemy"
}

public class BattleManager
{
    private Player player;
    private Enemy enemy;

    // Track throws per round during this encounter
    // Each round is a list of throws (usually 1, but can be multiple for special abilities)
    public List<List<Throws>> EncounterPlayerThrows { get; private set; } = new List<List<Throws>>();
    public List<List<Throws>> EncounterEnemyThrows { get; private set; } = new List<List<Throws>>();

    public BattleManager(Player player, Enemy enemy)
    {
        this.player = player;
        this.enemy = enemy;
    }

    public RoundResult ResolveRound(Throws playerThrow)
    {
        // Create result object
        var result = new RoundResult
        {
            PlayerThrow = playerThrow,
            EnemyThrow = Throws.rock, // Will be set below
            Outcome = RoundOutcome.Draw,
            DamageDealt = 0,
            DamageTarget = ""
        };

        // Pass player context so smarter enemies can react to player's previous behavior
        Throws enemyThrow = enemy.ChooseThrow(player.LastThrow, player.ThrowHistory, EncounterPlayerThrows, EncounterEnemyThrows);

        // Boss mechanic: throw twice and pick the best result
        bool isBossDraw = false; // Track if boss's best result is a draw
        if (enemy.isBoss)
        {
            Throws enemyThrow2 = enemy.ChooseThrow(player.LastThrow, player.ThrowHistory, EncounterPlayerThrows, EncounterEnemyThrows);
            // Ensure the two throws are different
            int attempts = 0;
            while (enemyThrow2 == enemyThrow && attempts < 10)
            {
                enemyThrow2 = enemy.ChooseThrow(player.LastThrow, player.ThrowHistory, EncounterPlayerThrows, EncounterEnemyThrows);
                attempts++;
            }

            // Determine which throw is better for the boss
            int result1 = CompareThrows(enemyThrow, playerThrow); // 1=enemy wins, 0=draw, -1=enemy loses
            int result2 = CompareThrows(enemyThrow2, playerThrow);

            GD.Print($"Boss threw: {enemyThrow} (result: {result1}) and {enemyThrow2} (result: {result2})");

            // Pick the throw with the better result for the boss
            int bestResult = Godot.Mathf.Max(result1, result2);

            if (bestResult == 0)
            {
                // Best result is a draw - player deals half damage to boss
                isBossDraw = true;
                enemyThrow = result1 == 0 ? enemyThrow : enemyThrow2;
                GD.Print($"Boss's best result is DRAW - player deals half damage");
            }
            else if (result1 > result2)
            {
                //enemyThrow = enemyThrow;
                GD.Print($"Boss chose {enemyThrow}");
            }
            else if (result2 > result1)
            {
                enemyThrow = enemyThrow2;
                GD.Print($"Boss chose {enemyThrow2}");
            }
            else
            {
                // If both results are equal, pick randomly
                enemyThrow = RngManager.Instance.Rng.Randf() > 0.5f ? enemyThrow : enemyThrow2;
                GD.Print($"Boss chose {enemyThrow} (random between equal results)");
            }
        }

        result.EnemyThrow = enemyThrow;
        GD.Print("Enemy Threw: " + enemyThrow.ToString());

        // Record throws for this round in the encounter history
        EncounterPlayerThrows.Add(new List<Throws> { playerThrow });
        EncounterEnemyThrows.Add(new List<Throws> { enemyThrow });

        bool playerWon = DetermineWinner(playerThrow, enemyThrow);

        // Override playerWon if boss draw - treat as player win with half damage
        if (isBossDraw)
        {
            playerWon = true;
        }

        if (playerWon)
        {
            // Damage is based on the player's move level (strength of the chosen move)
            var move = player.CurrentThrows.Find(m => m.Type == playerThrow);
            int baseDamage = move != null ? move.Level : 1;

            // Apply damage boost buffs
            int damageBoost = GameState.Instance.GetBuffAmount("damage_boost");
            int totalDamage = baseDamage + damageBoost;

            // Momentum enemy: damage is based on streak instead of base damage
            if (enemy.BehaviorName == "momentum")
            {
                int streak = MomentumBehavior.GetCurrentStreak(EncounterPlayerThrows, EncounterEnemyThrows);
                // Negative streak = player win streak (enemy loss streak)
                totalDamage = MomentumBehavior.GetStreakDamage(streak);
                GD.Print($"Momentum enemy - player streak: {streak}, damage: {totalDamage}");
            }

            // If boss draw, player deals half damage
            if (isBossDraw)
            {
                totalDamage = Godot.Mathf.Max(1, totalDamage / 2);
                GD.Print($"Boss draw - player deals half damage: {totalDamage}");
            }

            result.Outcome = RoundOutcome.PlayerWin;
            result.DamageDealt = totalDamage;
            result.DamageTarget = "enemy";

            GD.Print($"Player won the round. Base damage: {baseDamage}, Buff: +{damageBoost}, Total: {totalDamage}");
            enemy.TakeDamage(totalDamage);
        }
        else if (playerThrow != enemyThrow)
        {
            int incomingDamage = enemy.strength;

            // Momentum enemy: damage is based on streak
            if (enemy.BehaviorName == "momentum")
            {
                int streak = MomentumBehavior.GetCurrentStreak(EncounterPlayerThrows, EncounterEnemyThrows);
                // Positive streak = enemy win streak
                incomingDamage = MomentumBehavior.GetStreakDamage(streak);
                GD.Print($"Momentum enemy - enemy streak: {streak}, damage: {incomingDamage}");
            }

            // Apply damage reduction buffs
            int damageReduction = GameState.Instance.GetBuffAmount("damage_reduction");
            int finalDamage = Godot.Mathf.Max(0, incomingDamage - damageReduction);

            result.Outcome = RoundOutcome.EnemyWin;
            result.DamageDealt = finalDamage;
            result.DamageTarget = "player";

            GD.Print($"Player lost the round. Enemy damage: {incomingDamage}, Reduction: -{damageReduction}, Final: {finalDamage}");
            player.Damage(finalDamage);
            GD.Print($"Player health after Damage call: {GameState.Instance?.PlayerHealth}");
        }
        else
        {
            // Draw - no damage
            result.Outcome = RoundOutcome.Draw;
            GD.Print("Draw - no damage");

            // Allow enemy to react to draw
            enemy.OnDraw();
        }

        // After resolving the round, record the player's chosen throw for future rounds
        // (append to history and set LastThrow)
        player.ThrowHistory.Add(playerThrow);
        player.LastThrow = playerThrow;

        // Decrement buff durations after each round
        GameState.Instance.DecrementBuffDurations();

        // Allow special enemies to react
        //enemy.OnBattleResult(playerWon, this);

        return result;
    }

    private bool DetermineWinner(Throws player, Throws enemy)
    {
        if (player == enemy) return false;
        return (player == Throws.rock && enemy == Throws.scissors)
            || (player == Throws.paper && enemy == Throws.rock)
            || (player == Throws.scissors && enemy == Throws.paper);
    }

    // Compare throws from enemy's perspective: 1=enemy wins, 0=draw, -1=enemy loses
    private int CompareThrows(Throws enemyThrow, Throws playerThrow)
    {
        if (enemyThrow == playerThrow) return 0; // Draw

        bool enemyWins = (enemyThrow == Throws.rock && playerThrow == Throws.scissors)
                      || (enemyThrow == Throws.paper && playerThrow == Throws.rock)
                      || (enemyThrow == Throws.scissors && playerThrow == Throws.paper);

        return enemyWins ? 1 : -1;
    }
}
