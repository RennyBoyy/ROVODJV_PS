using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerUIAssigner : MonoBehaviour
{
    [Header("Player 1 UI")]
    public GameObject[] player1FruitObjects = new GameObject[5];
    public Sprite player1EmptySprite;

    [Header("Player 2 UI")]
    public GameObject[] player2FruitObjects = new GameObject[5];
    public Sprite player2EmptySprite;

    private void Start()
    {
        StartCoroutine(AssignUIToExistingPlayers());
    }

    private System.Collections.IEnumerator AssignUIToExistingPlayers()
    {
        yield return null;

        PlayerScript[] existingPlayers = FindObjectsByType<PlayerScript>(FindObjectsSortMode.None);

        foreach (PlayerScript player in existingPlayers)
        {
            PlayerInput playerInput = player.GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                Debug.Log($"Found existing player with index: {playerInput.playerIndex}");
                AssignUIToPlayer(playerInput);
            }
            else
            {
                Debug.Log($"Player found without PlayerInput, using LeftOrRight: {player.LeftOrRight}");
                if (player.LeftOrRight)     
                {
                    player.SetAmmoUI(player1FruitObjects, player1EmptySprite);
                }
                else     
                {
                    player.SetAmmoUI(player2FruitObjects, player2EmptySprite);
                }
            }
        }
    }

    private void OnEnable()
    {
        var manager = FindFirstObjectByType<PlayerInputManager>();
        if (manager != null)
            manager.onPlayerJoined += OnPlayerJoined;
    }

    private void OnDisable()
    {
        var manager = FindFirstObjectByType<PlayerInputManager>();
        if (manager != null)
            manager.onPlayerJoined -= OnPlayerJoined;
    }

    public void OnPlayerJoined(PlayerInput playerInput)
    {
        AssignUIToPlayer(playerInput);
    }

    private void AssignUIToPlayer(PlayerInput playerInput)
    {
        var playerScript = playerInput.GetComponent<PlayerScript>();
        if (playerScript != null)
        {
            Debug.Log($"Assigning UI to player {playerInput.playerIndex}");

            if (playerInput.playerIndex == 0)
            {
                playerScript.SetAmmoUI(player1FruitObjects, player1EmptySprite);
                playerScript.LeftOrRight = true;   
                Debug.Log("Assigned Player 1 UI");
            }
            else if (playerInput.playerIndex == 1)
            {
                playerScript.SetAmmoUI(player2FruitObjects, player2EmptySprite);
                playerScript.LeftOrRight = false;   
                Debug.Log("Assigned Player 2 UI");
            }
        }
        else
        {
            Debug.LogError("PlayerScript component not found on joined player!");
        }
    }
}