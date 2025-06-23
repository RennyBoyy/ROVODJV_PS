using System.Linq;
using UnityEngine;

public class SnowmanObstacke : MonoBehaviour
{
    [SerializeField] private GameObject[] snowmen;

    private int nextSnowmanToDie = 0;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log("Snowman obstacle triggered by " + other.name);

        // Get all currently active snowmen
        var activeSnowmen = snowmen.Where(s => s.activeSelf).ToList();
        if (activeSnowmen.Count == 0) return;

        // Randomly select one
        int randomIndex = Random.Range(0, activeSnowmen.Count);
        activeSnowmen[randomIndex].SetActive(false);
    }
}