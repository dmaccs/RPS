using System.Collections.Generic;
using Rps;

public interface IEnemyBehavior
{
    // Decide which throw the enemy will make.
    // previousPlayerThrow: the player's last throw (nullable)
    // playerHistory: list of previous player throws (may be null)
    // encounterPlayerThrows: list of lists - each round's player throws in this encounter
    // encounterEnemyThrows: list of lists - each round's enemy throws in this encounter
    Throws ChooseThrow(Throws? previousPlayerThrow, List<Throws> playerHistory,
                       List<List<Throws>> encounterPlayerThrows = null,
                       List<List<Throws>> encounterEnemyThrows = null);
}
