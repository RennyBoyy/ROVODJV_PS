using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Used for modular intro/target animation assignment in minigames


public class PlayerScript : MonoBehaviour
{
    [SerializeField] private GameObject projectile;
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
    public bool canMove = true;

    public int playerID;

    private bool insideReloadZone = false;
    [SerializeField] private GameManager_Fruity minigameManager;

    [Header("Player Identity")]
    [SerializeField] private PlayerIdentity playerIdentity;
    public PlayerIdentity PlayerType => playerIdentity;

    private PlayerInput playerInput;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        /*playerID = playerInput.playerIndex;
        playerIdentity = (PlayerIdentity)playerID;

        Debug.Log($"[PlayerController] Player {playerID} using device: {playerInput.devices[0].displayName}");*/
    }
    private void Start()
    {
        bullets = maxBullets;
        minigameManager = FindFirstObjectByType<GameManager_Fruity>();
        animator = GetComponent<Animator>();

        /*int index = playerInput.playerIndex;

        if (index == 0)
            playerInput.SwitchCurrentActionMap("Player");
        else if (index == 1)
            playerInput.SwitchCurrentActionMap("Player2");

        Debug.Log($"Player {index} using map: {playerInput.currentActionMap.name}");*/

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
                FruityGameConfigurator.Instance?.PlayJumpSound(playerIdentity == PlayerIdentity.Fruity);
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
                FruityGameConfigurator.Instance?.PlayJumpSound(playerIdentity == PlayerIdentity.Fruity);
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
            FruityGameConfigurator.Instance?.PlayEmptyThrowSound(playerIdentity == PlayerIdentity.Fruity);
        }
        else
        {
            if (projectile != null && hand != null)
            {
                GameObject proj = Instantiate(projectile, hand.transform.position, transform.rotation);
                Projectile projScript = proj.GetComponent<Projectile>();
                if (projScript != null)
                {
                    Vector3 dir = (playerIdentity == PlayerIdentity.Fruity) ? -Vector3.forward : Vector3.forward;
                    projScript.SetMoveDirection(dir);
                }
            }
            bullets--;
            UpdateAmmoUI();
            FruityGameConfigurator.Instance?.PlayThrowSound(playerIdentity == PlayerIdentity.Fruity);
        }
    }

    public void UpdateAmmoUI()
    {
        if (minigameManager == null) return;

        if (playerIdentity == PlayerIdentity.Fruity)
            minigameManager.UpdateP1AmmoUI(bullets);
        else
            minigameManager.UpdateP2AmmoUI(bullets);
    }

    private IEnumerator MoveLock()
    {
        yield return new WaitForSeconds(laneMoveDuration);
        canMove = true;
    }

  

    public void ReloadAmmo(int amount)
    {
        int spaceLeft = maxBullets - bullets;
        int ammoToGive = Mathf.Min(amount, spaceLeft);

        if (ammoToGive > 0)
        {
            bullets += ammoToGive;
            UpdateAmmoUI();
            FruityGameConfigurator.Instance?.PlayReloadSound(playerIdentity == PlayerIdentity.Fruity);
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

    /// <summary>
    /// Plays an intro/target animation by trigger name (for use in intro cinematics, etc).
    /// </summary>
    public void PlayIntroTargetAnimation(string triggerName)
    {
        if (animator != null && !string.IsNullOrEmpty(triggerName))
            animator.SetTrigger(triggerName);
    }

    /// <summary>
    /// Resets an intro/target animation trigger (call at end of intro to return to default state).
    /// </summary>
    public void ResetIntroTargetAnimation(string triggerName)
    {
        if (animator != null && !string.IsNullOrEmpty(triggerName))
            animator.ResetTrigger(triggerName);
    }
}