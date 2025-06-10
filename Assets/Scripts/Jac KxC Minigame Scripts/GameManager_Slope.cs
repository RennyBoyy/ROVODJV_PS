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

    void Start()
    {
        gameOverManager = GameOverManager.Instance;

        if (player1Controller == null || player2Controller == null)
        {
            FindPlayerControllers();
        }
    }

    void FindPlayerControllers()
    {
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        foreach (PlayerController player in players)
        {
            if (player.playerID == 1 && player1Controller == null)
            {
                player1Controller = player;
            }
            else if (player.playerID == 2 && player2Controller == null)
            {
                player2Controller = player;
            }
        }

        Debug.Log($"Found players: Player1 = {(player1Controller != null ? player1Controller.name : "None")}, Player2 = {(player2Controller != null ? player2Controller.name : "None")}");
    }

    void Update()
    {
        if (Fruit_Remaining <= 0 && !gameEnded)
        {
            loseGame();
        }
    }

    private void loseGame()
    {
        if (gameEnded) return;

        gameEnded = true;
        Debug.Log("You lose!");

        if (gameOverManager != null)
        {
            // Default: random player loses if not specified
            bool player1Lost = Random.Range(0, 2) == 0;
            gameOverManager.HandleGameEnd(player1Lost);
        }
        else
        {
            if (loseText != null)
                loseText.gameObject.SetActive(true);
            StartCoroutine(LoadSceneAfterDelay());
        }
    }

    // Call this to end the game from a specific player (e.g., when a player falls off)
    public void TriggerGameEndFromPlayer(PlayerController player)
    {
        if (gameEnded) return;

        gameEnded = true;
        Debug.Log("[GameManager_Slope] You lose! TriggerGameEndFromPlayer called.");

        bool player1Lost = (player == player1Controller);
        Debug.Log($"[GameManager_Slope] Calling HandleGameEnd. player1Lost={player1Lost}, player={player}");

        if (gameOverManager != null)
        {
            Debug.Log("[GameManager_Slope] gameOverManager is not null, calling HandleGameEnd.");
            gameOverManager.HandleGameEnd(player1Lost);
        }
        else
        {
            Debug.LogWarning("[GameManager_Slope] gameOverManager is null!");
            if (loseText != null)
                loseText.gameObject.SetActive(true);
            StartCoroutine(LoadSceneAfterDelay());
        }
    }

    private IEnumerator LoadSceneAfterDelay()
    {
        yield return new WaitForSeconds(3f);
        SceneManager.LoadScene("BugabooPlanet");
    }
}