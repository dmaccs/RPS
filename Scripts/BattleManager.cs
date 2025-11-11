using Godot;
using Rps;
public class BattleManager
{
    private Player player;
    private Enemy enemy;

    public BattleManager(Player player, Enemy enemy)
    {
        this.player = player;
        this.enemy = enemy;
    }

    public void ResolveRound(Throws playerThrow)
    {

    // Pass player context so smarter enemies can react to player's previous behavior
    Throws enemyThrow = enemy.ChooseThrow(player.LastThrow, player.ThrowHistory);
        GD.Print("Enemy Threw: " + enemyThrow.ToString());
        bool playerWon = DetermineWinner(playerThrow, enemyThrow);

        if (playerWon)
        {
            // Damage is based on the player's move level (strength of the chosen move)
            var move = player.CurrentThrows.Find(m => m.Type == playerThrow);
            int damage = move != null ? move.Level : 1;
            GD.Print($"Player won the round. Applying move damage: {damage}");
            enemy.TakeDamage(damage);
        }
        else if (playerThrow != enemyThrow)
        {
            GD.Print($"Player lost the round. Applying damage: {enemy.strength}");
            player.Damage(enemy.strength);
            GD.Print($"Player health after Damage call: {GameState.Instance?.PlayerHealth}");
        }

        // After resolving the round, record the player's chosen throw for future rounds
        // (append to history and set LastThrow)
        player.ThrowHistory.Add(playerThrow);
        player.LastThrow = playerThrow;

        // Allow special enemies to react
        //enemy.OnBattleResult(playerWon, this);
    }

    private bool DetermineWinner(Throws player, Throws enemy)
    {
        if (player == enemy) return false;
        return (player == Throws.rock && enemy == Throws.scissors)
            || (player == Throws.paper && enemy == Throws.rock)
            || (player == Throws.scissors && enemy == Throws.paper);
    }
}
