using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager_Fruity : MonoBehaviour
{
    public int Fruit_Remaining = 1;
    public bool gameActive = false;
    [SerializeField] private TextMeshProUGUI loseText;

    [Header("Player Detection")]
    [SerializeField] private Transform player1Transform;
    [SerializeField] private Transform player2Transform;

    private GameOverManager gameOverManager;
    private bool gameEnded = false;

    void Start()
    {
        gameOverManager = GameOverManager.Instance;

        if (player1Transform == null || player2Transform == null)
        {
            FindPlayerTransforms();
        }

        if (gameOverManager != null)
        {
            gameOverManager.SetPlayerTargets(player1Transform, player2Transform);
        }
    }

    void FindPlayerTransforms()
    {
        PlayerScript[] players = FindObjectsByType<PlayerScript>(FindObjectsSortMode.None);

        foreach (PlayerScript player in players)
        {
            if (player.LeftOrRight && player1Transform == null)
            {
                player1Transform = player.transform;
            }
            else if (!player.LeftOrRight && player2Transform == null)
            {
                player2Transform = player.transform;
            }
        }

        Debug.Log($"Found players: Player1 = {(player1Transform != null ? player1Transform.name : "None")}, Player2 = {(player2Transform != null ? player2Transform.name : "None")}");
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
            bool player1Lost = DetermineLosingPlayer(null); // Fallback to random if no MonsterBad instance is provided
            gameOverManager.HandleGameEnd(player1Lost);
        }
        else
        {
            if (loseText != null)
                loseText.gameObject.SetActive(true);
            StartCoroutine(LoadSceneAfterDelay());
        }
    }

    // Optionally keep as fallback, or remove if not needed
    private bool DetermineLosingPlayer(MonsterBad monster)
    {
        if (monster != null)
            return monster.didplayer1lose;
        return Random.Range(0, 2) == 0;
    }

    // Add this method to handle game end from a specific MonsterBad instance
    public void TriggerGameEndFromMonster(MonsterBad monster)
    {
        if (gameEnded) return;

        gameEnded = true;
        Debug.Log("[GameManager_Fruity] You lose! TriggerGameEndFromMonster called.");

        bool player1Lost = DetermineLosingPlayer(monster);
        Debug.Log($"[GameManager_Fruity] Calling HandleGameEnd. player1Lost={player1Lost}, monster={monster}");

        if (gameOverManager != null)
        {
            Debug.Log("[GameManager_Fruity] gameOverManager is not null, calling HandleGameEnd.");
            gameOverManager.HandleGameEnd(player1Lost);
        }
        else
        {
            Debug.LogWarning("[GameManager_Fruity] gameOverManager is null!");
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

    public void TriggerGameEnd(bool player1Lost)
    {
        if (gameEnded) return;

        gameEnded = true;

        if (gameOverManager != null)
        {
            gameOverManager.HandleGameEnd(player1Lost);
        }
        else
        {
            if (loseText != null)
                loseText.gameObject.SetActive(true);
            StartCoroutine(LoadSceneAfterDelay());
        }
    }
}