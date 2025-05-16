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
    public int maxBullets = 10;
    public TextMeshProUGUI ammoText;
    public TextMeshProUGUI fullAmmoText;

    public int bullets;
    private int currentLane = 3;

    private void Start()
    {
        bullets = maxBullets;
        UpdateAmmoUI();
        canMove = true;
        if (lanePoints != null && lanePoints.Length > 0)
        {
            transform.localPosition = lanePoints[currentLane].localPosition;
        }
    }

    private void Update()
    {
        HandleMovement();
        HandleShooting();
    }

    private void HandleMovement()
    {
        float move = Input.GetAxisRaw("Horizontal");

        if (move > 0.1f && canMove)
        {
            if (currentLane > 0)
            {
                currentLane--;
                MoveToLane(currentLane);
            }
            canMove = false;
            StartCoroutine(MoveLock());
        }
        else if (move < -0.1f && canMove)
        {
            if (currentLane < lanePoints.Length - 1)
            {
                currentLane++;
                MoveToLane(currentLane);
            }
            canMove = false;
            StartCoroutine(MoveLock());
        }
    }

    private void MoveToLane(int laneIndex)
    {
        if (lanePoints != null && laneIndex >= 0 && laneIndex < lanePoints.Length)
        {
            transform.position = lanePoints[laneIndex].position;
        }
    }

    private void HandleShooting()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Shoot();
        }
    }

    private void Shoot()
    {
        if (bullets <= 0)
        {
            Debug.Log("Out of Ammo");
            StartCoroutine(ShowOutOfAmmo());
        }
        else
        {
            Instantiate(tomato, transform.position, transform.rotation);
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
        yield return new WaitForSeconds(0.5f);
        canMove = true;
    }
}
