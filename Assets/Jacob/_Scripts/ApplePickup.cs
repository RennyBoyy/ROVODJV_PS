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
        StartCoroutine(WaitForPlayer());
    }

    private IEnumerator WaitForPlayer()
    {
        while (true)
        {
            player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerammo = player.GetComponent<PlayerScript>();
                if (playerammo != null)
                {
                    break;
                }
            }
            yield return null; 
        }

        if (ammoStages == null || ammoStages.Length == 0)
        {
            Debug.LogError("AmmoPile: No ammo stage prefabs assigned!");
            yield break;
        }

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

        int targetStage = Mathf.Min(Mathf.FloorToInt(fillAmount), ammoStages.Length - 1);

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
            var playerScript = other.GetComponent<PlayerScript>();
            if (playerScript != null)
            {
                CollectAmmo(playerScript);
            }
        }
    }

    private void CollectAmmo(PlayerScript playerScript)
    {
        int ammoAmount = Mathf.RoundToInt((currentStage + 1) * (10.0f / ammoStages.Length));

        if (playerScript != null && ammoAmount > 0)
        {
            int spaceLeft = playerScript.maxBullets - playerScript.bullets;
            int ammoToGive = Mathf.Min(ammoAmount, spaceLeft);

            if (ammoToGive > 0)
            {
                playerScript.bullets += ammoToGive;
                playerScript.UpdateAmmoUI();
                StartCoroutine(RespawnAmmoPile());
            }
            else
            {
                StartCoroutine(Ammofull(playerScript));
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

    private IEnumerator Ammofull(PlayerScript playerScript)
    {
        if (playerScript.fullAmmoText != null)
        {
            playerScript.fullAmmoText.text = "You cannot carry more fruit";
            playerScript.fullAmmoText.color = Color.red;
            playerScript.fullAmmoText.gameObject.SetActive(true);

            float fadeDuration = 3f;
            float elapsed = 0f;
            Color startColor = playerScript.fullAmmoText.color;
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

            while (elapsed < fadeDuration)
            {
                playerScript.fullAmmoText.color = Color.Lerp(startColor, endColor, elapsed / fadeDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            playerScript.fullAmmoText.gameObject.SetActive(false);
        }
    }
}
