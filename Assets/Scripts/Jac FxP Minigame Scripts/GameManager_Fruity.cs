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
    
    [SerializeField] private Sprite p1FullSprite; // P1 full ammo sprite
    [SerializeField] private Sprite p2FullSprite; // P2 full ammo sprite
    [SerializeField] private Sprite p1EmptySprite; // P1 empty sprite
    [SerializeField] private Sprite p2EmptySprite; // P2 empty sprite
    [SerializeField] private Image[] p1AmmoImages = new Image[5]; // P1 ammo images 1-5
    [SerializeField] private Image[] p2AmmoImages = new Image[5]; // P2 ammo images 1-5


    public bool IsGameDone { get; private set; }
    public int WinningPlayer { get; private set; } // 0 = player1, 1 = player2

    void Start()
    {
        if (player1Transform == null || player2Transform == null)
        {
            FindPlayerTransforms();
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
        
        // Stop all monsters from moving
        MonsterBad.StopAllMonsters();
        
        MonsterBad[] monsters = FindObjectsByType<MonsterBad>(FindObjectsSortMode.None);
        foreach (var monster in monsters)
            if (monster != null) monster.enabled = false;

        // Destroy all existing enemies
        foreach (var monster in monsters)
            if (monster != null) Destroy(monster.gameObject);

        // Notify GameManager (GameIntroManager)
        GameIntroManager.Instance.OnGameEnd(winningPlayer);
    }

    // Update P1 ammo UI (Fruity) - now left to right
    public void UpdateP1AmmoUI(int ammoCount)
    {
        for (int i = 0; i < p1AmmoImages.Length; i++)
        {
            if (p1AmmoImages[i] != null)
            {
                if (i < ammoCount)
                    p1AmmoImages[i].sprite = p1FullSprite;
                else
                    p1AmmoImages[i].sprite = p1EmptySprite;
            }
        }
    }

    // Update P2 ammo UI (Potato) - now right to left
    public void UpdateP2AmmoUI(int ammoCount)
    {
        for (int i = 0; i < p2AmmoImages.Length; i++)
        {
            if (p2AmmoImages[i] != null)
            {
                int rightToLeftIndex = p2AmmoImages.Length - 1 - i;
                if (rightToLeftIndex < ammoCount)
                    p2AmmoImages[i].sprite = p2FullSprite;
                else
                    p2AmmoImages[i].sprite = p2EmptySprite;
            }
        }
    }
}