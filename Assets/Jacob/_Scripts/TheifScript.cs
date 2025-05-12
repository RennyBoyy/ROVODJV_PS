using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class TheifScript : MonoBehaviour
{
    [SerializeField] private GameManager_Fruity gameManager;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private GameObject monsterPrefab;

    private bool canWave = true;

    void Start()
    {

    }

    // Update is called once per frame  
    void Update()
    {
        if (gameManager.gameActive && canWave)
        {
            StartCoroutine(WaveCoroutine(Random.Range(5f, 10f)));
        }

        if (gameManager.gameActive)
        {
            monsterPrefab.transform.position += -transform.forward * moveSpeed * Time.deltaTime;
        }
    }


    private void SpawnWaves()
    {
        monsterPrefab = Instantiate(monsterPrefab, transform.position, transform.rotation);

    }

    private IEnumerator WaveCoroutine(float waitTime)
    {
        canWave = false;
        SpawnWaves();

        yield return new WaitForSeconds(waitTime);

        canWave = true;
    }
}
