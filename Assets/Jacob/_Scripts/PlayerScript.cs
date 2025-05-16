using System.Collections;
using TMPro;
using UnityEngine;

// Pseudocode:
// - Track the current lane index (int currentLane).
// - On left input (move < -0.1f), increment currentLane (move right in array).
// - On right input (move > 0.1f), decrement currentLane (move left in array).
// - Clamp currentLane between 0 and lanePoints.Length - 1.
// - Set transform.localPosition to lanePoints[currentLane].localPosition.

public class PlayerScript : MonoBehaviour
{
    [SerializeField] private GameObject tomato;
    [SerializeField] private bool canMove = true;
    [SerializeField] private Transform[] lanePoints;
    [SerializeField] private Transform hand;
    public int maxBullets = 10;
    public TextMeshProUGUI ammoText;
    public TextMeshProUGUI fullAmmoText;

    public int bullets;
    private int currentLane = 3;

    // Lerp movement fields
    [SerializeField] private float laneMoveDuration = 0.7f;
    private bool isMoving = false;
    private Vector3 targetPosition;
    private float moveElapsed = 0f;
    private Vector3 startPosition;

    private Animator animator;

    private void Start()
    {
        bullets = maxBullets;
        UpdateAmmoUI();
        canMove = true;
        animator = GetComponent<Animator>();
       
            transform.localPosition = lanePoints[currentLane].localPosition;
       
       
    }

    private void Update()
    {
        HandleMovement();
        HandleShooting();
        HandleLerpMovement();
    }

    private void HandleMovement()
    {
        if (isMoving || !canMove) return;

        float move = Input.GetAxisRaw("Horizontal");

        if (move > 0.1f)
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
        else if (move < -0.1f)
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
        if (lanePoints != null && laneIndex >= 0 && laneIndex < lanePoints.Length)
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

    // 1. Remove Shoot() call from HandleShooting(), only trigger animation there.
    // 2. Create a public method Shoot() to be called by animation event at the end of "Throw" animation.

    private void HandleShooting()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            animator.SetTrigger("Throw");

        }
    }

    // This method should be called by an Animation Event at the end of the "Throw" animation.
    public void Shoot()
    {
        if (bullets <= 0)
        {
            Debug.Log("Out of Ammo");
            StartCoroutine(ShowOutOfAmmo());
        }
        else
        {
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
        if (ammoText != null)
        {
            fullAmmoText.text = "Out of Ammo!";
            fullAmmoText.color = Color.red;
            fullAmmoText.gameObject.SetActive(true);

            float fadeDuration = 3f;
            float elapsed = 0f;
            Color startColor = fullAmmoText.color;
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
            if (ammoText != null && fullAmmoText.rectTransform != null)
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
}
