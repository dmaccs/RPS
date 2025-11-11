using System.Collections.Generic;
using Rps;

public interface IEnemyBehavior
{
    // Decide which throw the enemy will make.
    // previousPlayerThrow: the player's last throw (nullable)
    // playerHistory: list of previous player throws (may be null)
    Throws ChooseThrow(Throws? previousPlayerThrow, List<Throws> playerHistory);
}
