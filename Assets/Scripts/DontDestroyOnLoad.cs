using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerIdentity { Fruity, Potato }

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    public struct PlayerData
    {
        public int playerIndex;
        public Gamepad gamepad;
        public PlayerIdentity identity;
    }

    private PlayerData[] players = new PlayerData[2];

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RegisterPlayer(int index, Gamepad gamepad, PlayerIdentity identity)
    {
        players[index] = new PlayerData
        {
            playerIndex = index,
            gamepad = gamepad,
            identity = identity
        };
    }

    public PlayerData GetPlayer(int index) => players[index];
}