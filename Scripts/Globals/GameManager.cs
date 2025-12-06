using Godot;
using System.Collections.Generic;
using System.Threading;

public partial class GameManager : Node
{

	public Player Player { get; private set; } = new Player();
    public bool IsTesting = false;
	public static GameManager Instance { get; private set; }
	private int stageIndex = 0;
	public int StageIndex => stageIndex; // Public read-only access to stageIndex
    private List<string> sceneOrder;
    private List<string> playTestOrder;

    public override void _Ready()
    {
        Instance = this;

        // Hardcoded progression
        sceneOrder = new List<string>
        {
            "res://Scenes/Combat/BattleScene.tscn",
            "res://Scenes/Transitions/Rewards.tscn",
            "res://Scenes/Combat/BattleScene.tscn",
            "res://Scenes/Transitions/Rewards.tscn",
            "res://Scenes/Shop/ShopScene.tscn",
            "res://Scenes/Combat/BattleScene.tscn",
            "res://Scenes/Transitions/Rewards.tscn",
            "res://Scenes/Combat/BattleScene.tscn",
            "res://Scenes/Transitions/Rewards.tscn",
            "res://Scenes/Events/EventScene.tscn",
             "res://Scenes/Combat/BattleScene.tscn",
             "res://Scenes/Transitions/Rewards.tscn",
             "res://Scenes/Combat/BattleScene.tscn",
             "res://Scenes/Transitions/Rewards.tscn",
             "res://Scenes/Shop/ShopScene.tscn",
             "res://Scenes/Combat/BattleScene.tscn",
             "res://Scenes/Transitions/Rewards.tscn",
             "res://Scenes/Events/EventScene.tscn",
             "res://Scenes/Combat/BattleScene.tscn",
             "res://Scenes/Transitions/Rewards.tscn",
             "res://Scenes/Rest/RestScene.tscn",
             "res://Scenes/Combat/BossScene.tscn",
             "res://Scenes/Transitions/VictoryScene.tscn",
        };
        
        playTestOrder = new List<string>
        {
            "res://Scenes/Combat/BattleScene.tscn",
            "res://Scenes/Transitions/Rewards.tscn",
            "res://Scenes/Combat/BattleScene.tscn",
            "res://Scenes/Transitions/Rewards.tscn",
            "res://Scenes/Shop/ShopScene.tscn",
            "res://Scenes/Combat/BattleScene.tscn",
            "res://Scenes/Transitions/Rewards.tscn",
            "res://Scenes/Combat/BattleScene.tscn",
            "res://Scenes/Transitions/Rewards.tscn",
            "res://Scenes/Events/EventScene.tscn",
            "res://Scenes/Combat/BattleScene.tscn",
            "res://Scenes/Transitions/Rewards.tscn",
            "res://Scenes/Combat/BattleScene.tscn",
            "res://Scenes/Transitions/Rewards.tscn",
            "res://Scenes/Shop/ShopScene.tscn",
            "res://Scenes/Combat/BattleScene.tscn",
            "res://Scenes/Transitions/Rewards.tscn",
            "res://Scenes/Events/EventScene.tscn",
            "res://Scenes/Combat/BattleScene.tscn",
            "res://Scenes/Transitions/Rewards.tscn",
            "res://Scenes/Rest/RestScene.tscn",
            "res://Scenes/Combat/BossScene.tscn",
            "res://Scenes/Transitions/VictoryScene.tscn",
        };

        // Initialize throws with starting set
        Player.InitializeThrows();

        LoadNextScene();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationPredelete)
        {
            if (Player != null)
            {
                Player.QueueFree();
                Player = null;
            }
        }
    }
    
    public void ResetGame()
    {
        // Reset GameState (health, gold, items, relics, etc.)
        if (GameState.Instance != null)
        {
            GameState.Instance.ResetGame();
        }

        // Free the old Player before creating a new one
        if (Player != null)
        {
            Player.QueueFree();
        }

        Player = new Player();

        // Initialize throws with starting set (new throw system)
        Player.InitializeThrows();

        stageIndex = 0;

        // Reset battle progression
        if (BattleProgressionManager.Instance != null)
        {
            BattleProgressionManager.Instance.ResetBattles();
        }

        LoadNextScene();
        GameState.Instance.RefreshUI();
    }

    public void LoadNextScene()
    {
        if (stageIndex >= sceneOrder.Count)
        {
            GD.Print("Game Over or Victory!");
            CallDeferred(nameof(DeferredChangeScene), "res://Scenes/Transitions/VictoryScene.tscn");
            return;
        }

        string nextScenePath = sceneOrder[stageIndex];

        // Increment stage counter if loading a BattleScene
        //if (nextScenePath.Contains("BattleScene") && BattleProgressionManager.Instance != null)
        //{
            BattleProgressionManager.Instance.IncrementStage(); //increment always for testing
        //}

        stageIndex++;
        CallDeferred(nameof(DeferredChangeScene), nextScenePath);
    }

    private void DeferredChangeScene(string scenePath)
    {
        GetTree().ChangeSceneToFile(scenePath);
    }
}
