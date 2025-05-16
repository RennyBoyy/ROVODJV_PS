using System.Collections;
using Unity.Hierarchy;
using UnityEngine;

public class AmmoPile : MonoBehaviour
{
    [Header("Ammo Pile Settings")]
    [SerializeField] private GameObject[] ammoStages;
    [SerializeField] private float fillRate = 0.1f;
    [SerializeField] private float respawnDelay = 3.0f;

    private PlayerScript playerammo;
    private GameObject player;
    private int currentStage = 0;
    private float fillAmount = 0f;
    private bool isGrowing = true;

    private void Start()
    {
        player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogError("AmmoPile: No GameObject with tag 'Player' found!");
            return;
        }

        playerammo = player.GetComponent<PlayerScript>();
        if (playerammo == null)
        {
            Debug.LogError("AmmoPile: Player GameObject does not have a PlayerScript component!");
            return;
        }

        if (ammoStages == null || ammoStages.Length == 0)
        {
            Debug.LogError("AmmoPile: No ammo stage prefabs assigned!");
            return;
        }

        // Hide all stages at start (empty pile)
        for (int i = 0; i < ammoStages.Length; i++)
        {
            if (ammoStages[i] != null)
            {
                ammoStages[i].SetActive(false);
            }
            else
            {
                Debug.LogWarning($"AmmoPile: Stage {i} is null!");
            }
        }

        currentStage = -1;
        fillAmount = 0f;
        isGrowing = true;
    }

    private void Update()
    {
        if (!isGrowing || ammoStages.Length == 0) return;

       
        fillAmount += fillRate * Time.deltaTime;

        // Calculate which stage should be active (0-based)
        int targetStage = Mathf.Min(Mathf.FloorToInt(fillAmount), ammoStages.Length - 1);

        // If we need to change the visible stage
        if (targetStage != currentStage)
        {
            for (int i = 0; i < ammoStages.Length; i++)
            {
                if (ammoStages[i] != null)
                    ammoStages[i].SetActive(i <= targetStage);
            }
            currentStage = targetStage;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CollectAmmo(other.gameObject);
        }
    }

    private void CollectAmmo(GameObject player)
    {
        int ammoAmount = Mathf.RoundToInt((currentStage + 1) * (10.0f / ammoStages.Length));

        if (playerammo != null && ammoAmount > 0)
        {
            int spaceLeft = playerammo.maxBullets - playerammo.bullets;
            int ammoToGive = Mathf.Min(ammoAmount, spaceLeft);

            if (ammoToGive > 0)
            {
                playerammo.bullets += ammoToGive;
                playerammo.UpdateAmmoUI();
                StartCoroutine(RespawnAmmoPile());
            }
            else
            {
                StartCoroutine(Ammofull());
            }
        }
    }

    private IEnumerator RespawnAmmoPile()
    {
        isGrowing = false;

        foreach (GameObject stage in ammoStages)
        {
            if (stage != null)
            {
                stage.SetActive(false);
            }
        }
        yield return new WaitForSeconds(respawnDelay);

        fillAmount = 0f;
        currentStage = -1;
        isGrowing = true;
    }

    private IEnumerator Ammofull()
    {
        if (playerammo.ammoText != null)
        {
            playerammo.fullAmmoText.text = "You cannot carry more fruit";
            playerammo.fullAmmoText.color = Color.red;
            playerammo.fullAmmoText.gameObject.SetActive(true);

            float fadeDuration = 3f;
            float elapsed = 0f;
            Color startColor = playerammo.fullAmmoText.color;
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
            if (playerammo.fullAmmoText != null && playerammo.ammoText.rectTransform != null)
            {
                playerammo.fullAmmoText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                playerammo.fullAmmoText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                playerammo.fullAmmoText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                playerammo.fullAmmoText.rectTransform.anchoredPosition = Vector2.zero;
            }
            while (elapsed < fadeDuration)
            {
                playerammo.fullAmmoText.color = Color.Lerp(startColor, endColor, elapsed / fadeDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            playerammo.fullAmmoText.gameObject.SetActive(false);
        }
    }
}
