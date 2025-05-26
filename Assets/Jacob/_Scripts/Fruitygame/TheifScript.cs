using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class TheifScript : MonoBehaviour
{
    [SerializeField] private GameManager_Fruity gameManager;
    
    [SerializeField] private GameObject monsterPrefab;

    [SerializeField] private GameObject[] monsterSpawner;

    private bool canWave = false;

    void Start()
    {
        canWave = true;
        int RandomSpawner = Random.Range(0, monsterSpawner.Length);
    }

    void Update()
    {
        if (gameManager.gameActive && canWave)
        {
            StartCoroutine(WaveCoroutine(Random.Range(5f, 10f)));
        }

        
    }


    private void SpawnWaves()
    {
      int RandomSpawner = Random.Range(0, monsterSpawner.Length);
      Instantiate(monsterPrefab, monsterSpawner[RandomSpawner].transform.position, transform.rotation);
    }

    private IEnumerator WaveCoroutine(float waitTime)
    {
        canWave = false;
        SpawnWaves();

        yield return new WaitForSeconds(waitTime);

        canWave = true;
    }
}
