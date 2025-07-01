using System.Collections;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager_Slope : MonoBehaviour
{
    public int Fruit_Remaining = 1;
    public bool gameActive = false;
    [SerializeField] private TextMeshProUGUI loseText;

    [Header("Player Detection")]
    [SerializeField] private PlayerController player1Controller;
    [SerializeField] private PlayerController player2Controller;

    private GameOverManager gameOverManager;
    private bool gameEnded = false;

    public bool IsGameDone { get; private set; }
    public int WinningPlayer { get; private set; }

    void Start()
    {
        gameOverManager = GameOverManager.Instance;

       
    }

    

    void Update()
    {
        if (gameActive)
        {

        }
    }

    

    // Call this to end the game from a specific player (e.g., when a player falls off)
    public void EndGame(int winningPlayer)
    {
        IsGameDone = true;
        WinningPlayer = winningPlayer;

        // Disable all gameplay scripts
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var player in players)
            if (player != null) player.enabled = false;

        // Notify GameManager (GameIntroManager)
        GameIntroManagerSKIGAME.Instance.OnGameEnd(winningPlayer);
    }

   
}