using System.Collections.Generic;

public class EnemyData
{
    //public string name { get; set; }
    public int health { get; set; }
    public int strength { get; set; }
    public int throwsPerTurn { get; set; }
    public List<string> allowedThrows { get; set; }
    public List<int> frequencies { get; set; }
    // Behavior hints: "random", "copy_last", "counter_most_common", or null/default
    public string behavior { get; set; }
    // Boss flag: if true, throws twice and picks best result
    public bool isBoss { get; set; } = false;
}
