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
    public bool LeftOrRight;

    private GameObject[] fruitUIObjects = new GameObject[5];
    private Sprite emptyFruitSprite;
    private Sprite[] originalSprites = new Sprite[5];
    private Image[] fruitImages = new Image[5];

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

    private void Start()
    {
        bullets = maxBullets;

        for (int i = 0; i < fruitUIObjects.Length; i++)
        {
            if (fruitUIObjects[i] != null)
            {
                fruitImages[i] = fruitUIObjects[i].GetComponent<Image>();
                if (fruitImages[i] != null)
                {
                    originalSprites[i] = fruitImages[i].sprite;
                }
            }
        }

        UpdateAmmoUI();
        animator = GetComponent<Animator>();

        var gamepads = Gamepad.all;
        if (player1Input != null && gamepads.Count > 0)
        {
            player1Input.SwitchCurrentControlScheme("Gamepad", gamepads[0]);
            player1Input.ActivateInput();
        }

        if (player2Input != null && gamepads.Count > 1)
        {
            player2Input.SwitchCurrentControlScheme("Gamepad", gamepads[1]);
            player2Input.ActivateInput();
        }
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
                FruityGameConfigurator.Instance?.PlayJumpSound(LeftOrRight);
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
                FruityGameConfigurator.Instance?.PlayJumpSound(LeftOrRight);
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
            FruityGameConfigurator.Instance?.PlayEmptyThrowSound(LeftOrRight);
        }
        else
        {
            if (tomato != null && hand != null)
                Instantiate(tomato, hand.transform.position, transform.rotation);
            bullets--;
            UpdateAmmoUI();
            FruityGameConfigurator.Instance?.PlayThrowSound(LeftOrRight);
        }
    }

    public void UpdateAmmoUI()
    {
        for (int i = 0; i < fruitImages.Length; i++)
        {
            if (fruitImages[i] != null)
            {
                if (i < bullets)
                {
                    fruitImages[i].sprite = originalSprites[i];
                }
                else
                {
                    fruitImages[i].sprite = emptyFruitSprite;
                }
            }
        }
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
            if (playerInput.playerIndex == 0)
            {
                playerScript.LeftOrRight = true;
            }
            else if (playerInput.playerIndex == 1)
            {
                playerScript.LeftOrRight = false;
            }
        }
    }

    public void SetAmmoUI(GameObject[] uiObjects, Sprite emptySprite)
    {
        fruitUIObjects = uiObjects;
        emptyFruitSprite = emptySprite;

        for (int i = 0; i < fruitUIObjects.Length; i++)
        {
            if (fruitUIObjects[i] != null)
            {
                fruitImages[i] = fruitUIObjects[i].GetComponent<Image>();
                if (fruitImages[i] != null)
                {
                    originalSprites[i] = fruitImages[i].sprite;
                }
            }
        }
        UpdateAmmoUI();
    }

    public void ReloadAmmo(int amount)
    {
        int spaceLeft = maxBullets - bullets;
        int ammoToGive = Mathf.Min(amount, spaceLeft);

        if (ammoToGive > 0)
        {
            bullets += ammoToGive;
            UpdateAmmoUI();
            FruityGameConfigurator.Instance?.PlayReloadSound(LeftOrRight);
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
}