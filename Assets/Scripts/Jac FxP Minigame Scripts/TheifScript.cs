using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class TheifScript : MonoBehaviour
{
    [SerializeField] private GameManager_Fruity gameManager;
    [SerializeField] private GameObject monsterPrefab;
    [SerializeField] private GameObject[] monsterSpawner;

    public bool canWave = false;

    [Header("Difficulty Increase")]
    [SerializeField] private float minSpawnInterval = 5f;
    [SerializeField] private float maxSpawnInterval = 10f;
    [SerializeField] private float spawnIntervalDecrease = 0.5f; 
    [SerializeField] private int monstersPerWave = 1;
    [SerializeField] private int maxMonstersPerWave = 10;
    [SerializeField] private float difficultyIncreaseInterval = 15f; 

    private float nextDifficultyIncreaseTime = 0f;

    void Start()
    {
        canWave = false;
        nextDifficultyIncreaseTime = Time.time + difficultyIncreaseInterval;
    }

    void Update()
    {
        if (gameManager.gameActive && canWave)
        {
            canWave = false; // Prevent multiple coroutines
            StartCoroutine(WaveCoroutine(Random.Range(minSpawnInterval, maxSpawnInterval)));
        }

    
        if (gameManager.gameActive && Time.time >= nextDifficultyIncreaseTime)
        {
            minSpawnInterval = Mathf.Max(1f, minSpawnInterval - spawnIntervalDecrease);
            maxSpawnInterval = Mathf.Max(2f, maxSpawnInterval - spawnIntervalDecrease);
            
            nextDifficultyIncreaseTime = Time.time + difficultyIncreaseInterval;
        }
    }

    private void SpawnWaves()
    {
        for (int i = 0; i < monstersPerWave; i++)
        {
            int randomSpawner = Random.Range(0, monsterSpawner.Length);
            Instantiate(monsterPrefab, monsterSpawner[randomSpawner].transform.position, transform.rotation);
            Debug.Log($"{gameObject.name} spawned a monster at {monsterSpawner[randomSpawner].name}");

            ParticleSystem ps = monsterSpawner[randomSpawner].GetComponent<ParticleSystem>();
            if (ps == null)
                ps = monsterSpawner[randomSpawner].GetComponentInChildren<ParticleSystem>();
            if (ps != null)
                ps.Play();
        }
    }

    private IEnumerator WaveCoroutine(float waitTime)
    {
        canWave = false;
        SpawnWaves();

        yield return new WaitForSeconds(waitTime);

        canWave = true;
    }
}
