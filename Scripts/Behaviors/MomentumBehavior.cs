using System.Collections.Generic;
using Godot;
using Rps;

namespace RPS.Scripts.Behaviors
{
    public class MomentumBehavior : IEnemyBehavior
    {
        private RandomNumberGenerator rng;
        private List<Throws> allowed;

        public MomentumBehavior(List<Throws> allowed, RandomNumberGenerator rng)
        {
            this.allowed = allowed ?? new List<Throws> { Throws.rock, Throws.paper, Throws.scissors };
            this.rng = rng ?? new RandomNumberGenerator();
        }

        public Throws ChooseThrow(Throws? previousPlayerThrow, List<Throws> playerHistory,
                                  List<List<Throws>> encounterPlayerThrows = null,
                                  List<List<Throws>> encounterEnemyThrows = null)
        {
            // Always random
            if (allowed.Count == 0) return Throws.rock;
            int idx = rng.RandiRange(0, allowed.Count - 1);
            return allowed[idx];
        }

        /// <summary>
        /// Calculate the current momentum streak from encounter history.
        /// Positive = enemy win streak, Negative = enemy loss streak.
        /// Draws reset the streak to 0.
        /// </summary>
        public static int GetCurrentStreak(List<List<Throws>> encounterPlayerThrows, List<List<Throws>> encounterEnemyThrows)
        {
            if (encounterPlayerThrows == null || encounterEnemyThrows == null)
                return 0;

            int streak = 0;

            // Count from the end, stop at any draw
            for (int i = encounterPlayerThrows.Count - 1; i >= 0; i--)
            {
                if (i >= encounterEnemyThrows.Count) continue;

                var playerThrows = encounterPlayerThrows[i];
                var enemyThrows = encounterEnemyThrows[i];

                if (playerThrows.Count == 0 || enemyThrows.Count == 0) continue;

                int result = CompareThrows(enemyThrows[0], playerThrows[0]);

                if (result == 0)
                {
                    // Draw resets streak - stop counting
                    break;
                }

                if (streak == 0)
                {
                    streak = result;
                }
                else if ((streak > 0 && result > 0) || (streak < 0 && result < 0))
                {
                    streak += result;
                }
                else
                {
                    // Streak direction changed - stop
                    break;
                }
            }

            return streak;
        }

        /// <summary>
        /// Get damage bonus based on streak length.
        /// Streak of 1 = 1, streak of 2 = 2, etc.
        /// </summary>
        public static int GetStreakDamage(int streak)
        {
            return Mathf.Abs(streak);
        }

        private static int CompareThrows(Throws enemyThrow, Throws playerThrow)
        {
            if (enemyThrow == playerThrow) return 0;

            bool enemyWins = (enemyThrow == Throws.rock && playerThrow == Throws.scissors)
                          || (enemyThrow == Throws.paper && playerThrow == Throws.rock)
                          || (enemyThrow == Throws.scissors && playerThrow == Throws.paper);

            return enemyWins ? 1 : -1;
        }
    }
}
