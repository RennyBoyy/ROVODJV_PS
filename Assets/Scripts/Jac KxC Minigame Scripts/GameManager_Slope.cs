using System.Collections;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager_Slope : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI loseText;

    [Header("Player Detection")]
    private int playersFinished = 0;
    private int firstPlayerID = -1;
    private Animator player1Animator;
    private Animator player2Animator;

    private bool gameEnded = false;

    public bool IsGameDone { get; private set; }
    public int WinningPlayer { get; private set; }

    void Start()
    {
       
    }

    
    public void restartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void MainMenu()
    {
        SceneManager.LoadScene("MAIN Bugaboo Planet");
    }
    public void PlayerReachedGoal(int playerID, Animator playerAnimator)
    {
        playersFinished++;

        // Fix: assign animator to the correct slot based on player ID
        if (playerID == 0) player1Animator = playerAnimator;
        else if (playerID == 1) player2Animator = playerAnimator;

        // First to finish
        if (firstPlayerID == -1)
        {
            firstPlayerID = playerID;
            Debug.Log($"Player {playerID} was first to reach the goal.");
        }

        // End game once both arrived
        if (playersFinished >= 2 && !gameEnded)
        {
            gameEnded = true;
            int winningPlayer = firstPlayerID;
            EndGame(winningPlayer);
            if (player1Animator != null)
                player1Animator?.ResetTrigger("Stop");
            if (player2Animator != null)
                player2Animator?.ResetTrigger("Stop");

        }
    }

    public void EndGame(int winningPlayer)
    {
        Debug.Log($"[GameManager_Slope] Ending game. Winning Player: {winningPlayer}");
        IsGameDone = true;
        WinningPlayer = winningPlayer;

        if (player1Animator != null)
            player1Animator.SetTrigger(winningPlayer == 0 ? "Victory" : "Defeat");
        if (player2Animator != null)
            player2Animator.SetTrigger(winningPlayer == 1 ? "Victory" : "Defeat");

        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var player in players)
            if (player != null) player.enabled = false;

        GameIntroManagerSKIGAME.Instance.OnGameEnd(winningPlayer);
    }



}