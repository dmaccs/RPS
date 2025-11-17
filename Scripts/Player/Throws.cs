namespace Rps
{
    public enum Throws
    {
        rock, paper, scissors, turkey, dynamite, wizard, lizard, spock
    }
    
        public class MoveData
    {
        public Throws Type { get; private set; }
        public int Level { get; set; }

        // You can add more properties here later (cooldowns, effects, etc.)
        public MoveData(Throws type, int level = 1)
        {
            Type = type;
            Level = level;
        }
    }
}