using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerScript : MonoBehaviour
{
    [SerializeField] private GameObject tomato;
    [SerializeField] private Transform[] lanePoints;
    [SerializeField] private Transform hand;
    [SerializeField] private string laneGroupName = "Collliders_Lives";
    public PlayerInput player1Input;
    public PlayerInput player2Input;
    public int maxBullets = 5;

    public int bullets;
    private int currentLane = 3;
    [SerializeField] private float laneMoveDuration = 0.7f;
    private bool isMoving = false;
    private Vector3 targetPosition;
    private float moveElapsed = 0f;
    private Vector3 startPosition;

    private Animator animator;
    private float moveInput;
    private bool shootInput;
    private bool canMove = true;

    private bool insideReloadZone = false;
    private GameManager_Fruity minigameManager;

    private void Start()
    {
        bullets = maxBullets;

        // Find MinigameManager for UI updates
        minigameManager = FindFirstObjectByType<GameManager_Fruity>();

        animator = GetComponent<Animator>();

        // Find this player's index to assign correct controller
        PlayerScript[] allPlayers = FindObjectsByType<PlayerScript>(FindObjectsSortMode.None);
        int playerIndex = -1;
        for (int i = 0; i < allPlayers.Length; i++)
        {
            if (allPlayers[i] == this)
            {
                playerIndex = i;
                break;
            }
        }

        var gamepads = Gamepad.all;
        if (playerIndex == 0 && gamepads.Count > 0) // Fruity (P1)
        {
            if (player1Input != null)
            {
                player1Input.SwitchCurrentControlScheme("Gamepad", gamepads[0]);
                player1Input.ActivateInput();
            }
        }
        else if (playerIndex == 1 && gamepads.Count > 1) // Potato (P2)
        {
            if (player2Input != null)
            {
                player2Input.SwitchCurrentControlScheme("Gamepad", gamepads[1]);
                player2Input.ActivateInput();
            }
        }

        // Update initial ammo UI
        UpdateAmmoUI();
    }

    private void Update()
    {
        HandleMovement();
        HandleLerpMovement();

        if (shootInput && !insideReloadZone)
        {
            if (animator != null)
                animator.SetTrigger("Throw");
            shootInput = false;
        }
        else
        {
            animator.ResetTrigger("Throw");
        }
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        Vector2 input = ctx.ReadValue<Vector2>();
        input.x = 0;
        moveInput = input.y;
        Debug.Log(input);
    }

    public void OnAttack(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
            shootInput = true;
    }

    private void HandleMovement()
    {
        if (isMoving || !canMove) return;

        if (moveInput > 0.1f)
        {
            if (currentLane > 0)
            {
                currentLane--;
                StartLerpToLane(currentLane);
                TriggerMoveAnimation(-1);
                FruityGameConfigurator.Instance?.PlayJumpSound(IsPlayer1());
            }
            canMove = false;
            StartCoroutine(MoveLock());
        }
        else if (moveInput < -0.1f)
        {
            if (currentLane < lanePoints.Length - 1)
            {
                currentLane++;
                StartLerpToLane(currentLane);
                TriggerMoveAnimation(1);
                FruityGameConfigurator.Instance?.PlayJumpSound(IsPlayer1());
            }
            canMove = false;
            StartCoroutine(MoveLock());
        }
    }

    private void StartLerpToLane(int laneIndex)
    {
        if (lanePoints != null && laneIndex >= 0 && laneIndex < lanePoints.Length && lanePoints[laneIndex] != null)
        {
            startPosition = transform.position;
            targetPosition = lanePoints[laneIndex].position;
            moveElapsed = 0f;
            isMoving = true;
        }
    }

    private void HandleLerpMovement()
    {
        if (!isMoving) return;

        moveElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(moveElapsed / laneMoveDuration);
        transform.position = Vector3.Lerp(startPosition, targetPosition, t);

        if (t >= 1f)
        {
            isMoving = false;
            transform.position = targetPosition;
        }
    }

    private void TriggerMoveAnimation(int direction)
    {
        if (animator != null)
        {
            animator.SetTrigger(direction > 0 ? "JumpRight" : "JumpLeft");
        }
    }

    public void Shoot()
    {
        if (insideReloadZone)
        {
            Debug.Log("Cannot throw while reloading.");
            return;
        }

        if (bullets <= 0)
        {
            Debug.Log("Out of Ammo");
            FruityGameConfigurator.Instance?.PlayEmptyThrowSound(IsPlayer1());
        }
        else
        {
            if (tomato != null && hand != null)
                Instantiate(tomato, hand.transform.position, transform.rotation);
            bullets--;
            UpdateAmmoUI();
            FruityGameConfigurator.Instance?.PlayThrowSound(IsPlayer1());
        }
    }

    public void UpdateAmmoUI()
    {
        if (minigameManager == null) return;

        if (IsPlayer1())
            minigameManager.UpdateP1AmmoUI(bullets);
        else
            minigameManager.UpdateP2AmmoUI(bullets);
    }

    private IEnumerator MoveLock()
    {
        yield return new WaitForSeconds(laneMoveDuration);
        canMove = true;
    }

    public void OnPlayerJoined(PlayerInput playerInput)
    {
        var playerScript = playerInput.GetComponent<PlayerScript>();
        if (playerScript != null)
        {
            // Player identity is now determined by index, not LeftOrRight
            // Index 0 = Fruity (P1, left), Index 1 = Potato (P2, right)
        }
    }

    public void ReloadAmmo(int amount)
    {
        int spaceLeft = maxBullets - bullets;
        int ammoToGive = Mathf.Min(amount, spaceLeft);

        if (ammoToGive > 0)
        {
            bullets += ammoToGive;
            UpdateAmmoUI();
            FruityGameConfigurator.Instance?.PlayReloadSound(IsPlayer1());
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Tomato"))
        {
            insideReloadZone = true;
            Debug.Log("Entered reload zone – cannot throw now.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Tomato"))
        {
            insideReloadZone = false;
            Debug.Log("Exited reload zone – can throw again.");
        }
    }

    private bool IsPlayer1()
    {
        PlayerScript[] allPlayers = FindObjectsByType<PlayerScript>(FindObjectsSortMode.None);
        for (int i = 0; i < allPlayers.Length; i++)
        {
            if (allPlayers[i] == this)
            {
                return i == 0; // Index 0 = Fruity (P1), Index 1 = Potato (P2)
            }
        }
        return false; // Fallback
    }
}