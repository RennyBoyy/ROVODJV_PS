
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerUIAssigner : MonoBehaviour
{
    [Header("Player 1 UI")]
    public TextMeshProUGUI player1AmmoText;
    public TextMeshProUGUI player1EmptyText;

    [Header("Player 2 UI")]
    public TextMeshProUGUI player2AmmoText;
    public TextMeshProUGUI player2EmptyText;

    private void OnEnable()
    {
        // Register to the PlayerInputManager event
        var manager = FindObjectOfType<PlayerInputManager>();
        if (manager != null)
            manager.onPlayerJoined += OnPlayerJoined;
    }

    private void OnDisable()
    {
        // Unregister to avoid memory leaks
        var manager = FindObjectOfType<PlayerInputManager>();
        if (manager != null)
            manager.onPlayerJoined -= OnPlayerJoined;
    }

    public void OnPlayerJoined(PlayerInput playerInput)
    {
        var playerScript = playerInput.GetComponent<PlayerScript>();
        if (playerScript != null)
        {
            if (playerInput.playerIndex == 0)
                playerScript.SetAmmoUI(player1AmmoText, player1EmptyText);
            else if (playerInput.playerIndex == 1)
                playerScript.SetAmmoUI(player2AmmoText, player2EmptyText);
        }
    }
}
