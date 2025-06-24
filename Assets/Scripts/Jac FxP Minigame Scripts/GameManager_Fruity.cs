using System.Collections;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager_Fruity : MonoBehaviour
{
    public bool gameActive = false;

    [Header("Player Detection")]
    [SerializeField] private Transform player1Transform;
    [SerializeField] private Transform player2Transform;

    [Header("UI Assignment")]

    [Header("Ammo UI Images")]
    [SerializeField] private Image[] p1AmmoImages = new Image[5]; // P1 ammo images 1-5
    [SerializeField] private Image[] p2AmmoImages = new Image[5]; // P2 ammo images 1-5
    [SerializeField] private Sprite p1EmptySprite; // P1 empty sprite
    [SerializeField] private Sprite p2EmptySprite; // P2 empty sprite

    // Store original sprites for each ammo image
    private Sprite[] p1OriginalSprites = new Sprite[5];
    private Sprite[] p2OriginalSprites = new Sprite[5];

    private bool gameEnded = false;

    public bool IsGameDone { get; private set; }
    public int WinningPlayer { get; private set; } // 0 = player1, 1 = player2

    void Start()
    {
        if (player1Transform == null || player2Transform == null)
        {
            FindPlayerTransforms();
        }

        // Cache original sprites for each ammo image
        CacheOriginalSprites();
    }

    private void CacheOriginalSprites()
    {
        // Cache P1 ammo original sprites
        for (int i = 0; i < p1AmmoImages.Length; i++)
        {
            if (p1AmmoImages[i] != null)
                p1OriginalSprites[i] = p1AmmoImages[i].sprite;
        }

        // Cache P2 ammo original sprites
        for (int i = 0; i < p2AmmoImages.Length; i++)
        {
            if (p2AmmoImages[i] != null)
                p2OriginalSprites[i] = p2AmmoImages[i].sprite;
        }
    }

    void FindPlayerTransforms()
    {
        PlayerScript[] players = FindObjectsByType<PlayerScript>(FindObjectsSortMode.None);

        for (int i = 0; i < players.Length; i++)
        {
            if (i == 0 && player1Transform == null)
            {
                player1Transform = players[i].transform;
            }
            else if (i == 1 && player2Transform == null)
            {
                player2Transform = players[i].transform;
            }
        }

        Debug.Log($"Found players: Player1 = {(player1Transform != null ? player1Transform.name : "None")}, Player2 = {(player2Transform != null ? player2Transform.name : "None")}");
    }

    // Game ends only when monsters reach lose triggers (handled by MonsterBad script)
    // No need for redundant ammo-based lose condition

    public void EndGame(int winningPlayer)
    {
        IsGameDone = true;
        WinningPlayer = winningPlayer;

        // Disable all gameplay scripts
        PlayerScript[] players = FindObjectsByType<PlayerScript>(FindObjectsSortMode.None);
        foreach (var player in players)
            if (player != null) player.enabled = false;
        TheifScript[] thieves = FindObjectsByType<TheifScript>(FindObjectsSortMode.None);
        foreach (var thief in thieves)
            if (thief != null) thief.enabled = false;
        MonsterBad.isMoving = false;
        MonsterBad[] monsters = FindObjectsByType<MonsterBad>(FindObjectsSortMode.None);
        foreach (var monster in monsters)
            if (monster != null) monster.enabled = false;

        // Destroy all existing enemies
        foreach (var monster in monsters)
            if (monster != null) Destroy(monster.gameObject);

        // Notify GameManager (GameIntroManager)
        GameIntroManager.Instance.OnGameEnd(winningPlayer);
    }

    // Update P1 ammo UI (Fruity) - replaces from right to left
    public void UpdateP1AmmoUI(int ammoCount)
    {
        for (int i = 0; i < p1AmmoImages.Length; i++)
        {
            if (p1AmmoImages[i] != null)
            {
                // Right to left: index 4 is rightmost, index 0 is leftmost
                int rightToLeftIndex = 4 - i;
                if (rightToLeftIndex < ammoCount)
                    p1AmmoImages[i].sprite = p1OriginalSprites[i];
                else
                    p1AmmoImages[i].sprite = p1EmptySprite;
            }
        }
    }

    // Update P2 ammo UI (Potato) - replaces from left to right
    public void UpdateP2AmmoUI(int ammoCount)
    {
        for (int i = 0; i < p2AmmoImages.Length; i++)
        {
            if (p2AmmoImages[i] != null)
            {
                // Left to right: index 0 is leftmost, index 4 is rightmost
                if (i < ammoCount)
                    p2AmmoImages[i].sprite = p2OriginalSprites[i];
                else
                    p2AmmoImages[i].sprite = p2EmptySprite;
            }
        }
    }
}