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
            bool player1Lost = DetermineLosingPlayer();
            gameOverManager.HandleGameEnd(player1Lost);
        }
        else
        {
            if (loseText != null)
                loseText.gameObject.SetActive(true);
            StartCoroutine(LoadSceneAfterDelay());
        }
    }

    private bool DetermineLosingPlayer()
    {
        return Random.Range(0, 2) == 0;
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