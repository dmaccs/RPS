namespace RPS.Scripts.Behaviors;
using System.Collections.Generic;
using System;
using Godot;
using Rps;

public class NotSeenBehavior : IEnemyBehavior
{
    private RandomNumberGenerator rng;
    private List<Throws> allowed;

    public NotSeenBehavior(List<Throws> allowed, RandomNumberGenerator rng)
    {
        this.allowed = allowed ?? new List<Throws> { Throws.rock, Throws.paper, Throws.scissors };
        this.rng = rng ?? new RandomNumberGenerator();
    }

    public Throws ChooseThrow(Throws? previousPlayerThrow, List<Throws> playerHistory,
                              List<List<Throws>> encounterPlayerThrows = null,
                              List<List<Throws>> encounterEnemyThrows = null)
    {
        // If we have encounter history, choose from throws not seen in the previous round
        if (encounterPlayerThrows != null && encounterEnemyThrows != null &&
            encounterPlayerThrows.Count > 0 && encounterEnemyThrows.Count > 0)
        {
            // Get all throws from the previous round (both player and enemy)
            var seenThrows = new HashSet<Throws>();
            var lastPlayerRound = encounterPlayerThrows[encounterPlayerThrows.Count - 1];
            var lastEnemyRound = encounterEnemyThrows[encounterEnemyThrows.Count - 1];

            foreach (var t in lastPlayerRound)
                seenThrows.Add(t);
            foreach (var t in lastEnemyRound)
                seenThrows.Add(t);

            // Filter allowed throws to only include unseen ones
            var unseenThrows = new List<Throws>();
            foreach (var t in allowed)
            {
                if (!seenThrows.Contains(t))
                    unseenThrows.Add(t);
            }

            // If we have unseen throws, pick randomly from them
            if (unseenThrows.Count > 0)
            {
                int idx = rng.RandiRange(0, unseenThrows.Count - 1);
                return unseenThrows[idx];
            }
        }

        // Fallback to random allowed throw
        if (allowed.Count == 0) return Throws.rock;
        int idx2 = rng.RandiRange(0, allowed.Count - 1);
        return allowed[idx2];
    }
}
