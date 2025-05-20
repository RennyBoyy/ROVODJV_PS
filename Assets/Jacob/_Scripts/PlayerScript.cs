using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerScript : MonoBehaviour
{
    [SerializeField] private GameObject tomato;
    [SerializeField] private Transform[] lanePoints;
    [SerializeField] private Transform hand;
    [SerializeField] private string laneGroupName = "Collliders_Lives";
    public int maxBullets = 10;
    public TextMeshProUGUI ammoText;
    public TextMeshProUGUI fullAmmoText;

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

    private void Start()
    {
        bullets = maxBullets;
        UpdateAmmoUI();
        animator = GetComponent<Animator>();

        Transform laneGroup = GameObject.Find(laneGroupName)?.transform;
        if (laneGroup != null)
        {
            lanePoints = new Transform[laneGroup.childCount];
            for (int i = 0; i < laneGroup.childCount; i++)
            {
                lanePoints[i] = laneGroup.GetChild(i);
            }
        }
        else
        {
            Debug.LogError("Lane group not found: " + laneGroupName);
        }
    }
   

    private void Update()
    {
        HandleMovement();
        HandleLerpMovement();

        if (shootInput)
        {
            if (animator != null)
                animator.SetTrigger("Throw");
            shootInput = false;
        }
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>().x;
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

    // Called by animation event in throw animation
    public void Shoot()
    {
        if (bullets <= 0)
        {
            Debug.Log("Out of Ammo");
            StartCoroutine(ShowOutOfAmmo());
        }
        else
        {
            if (tomato != null && hand != null)
                Instantiate(tomato, hand.transform.position, transform.rotation);
            bullets--;
            UpdateAmmoUI();
        }
    }

    public void UpdateAmmoUI()
    {
        if (ammoText != null)
        {
            ammoText.text = $"Ammo: {bullets}/{maxBullets}";
            ammoText.color = Color.black;
            ammoText.gameObject.SetActive(true);
        }
    }

    private IEnumerator ShowOutOfAmmo()
    {
        if (fullAmmoText != null)
        {
            fullAmmoText.text = "Out of Ammo!";
            fullAmmoText.color = Color.red;
            fullAmmoText.gameObject.SetActive(true);

            float fadeDuration = 3f;
            float elapsed = 0f;
            Color startColor = fullAmmoText.color;
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

            if (fullAmmoText.rectTransform != null)
            {
                fullAmmoText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                fullAmmoText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                fullAmmoText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                fullAmmoText.rectTransform.anchoredPosition = Vector2.zero;
            }
            while (elapsed < fadeDuration)
            {
                fullAmmoText.color = Color.Lerp(startColor, endColor, elapsed / fadeDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            fullAmmoText.gameObject.SetActive(false);
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
                playerScript.laneGroupName = "Collliders_Lives";
            else if (playerInput.playerIndex == 1)
                playerScript.laneGroupName = "Collliders_Lives (1)";
            // ...and so on for more players
        }
    }
    public void SetAmmoUI(TextMeshProUGUI ammo, TextMeshProUGUI empty)
    {
        ammoText = ammo;
        fullAmmoText = empty;
    }
}

    
