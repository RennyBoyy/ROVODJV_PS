using System.Linq;
using UnityEngine;

public class SnowmanObstacke : MonoBehaviour
{
    [SerializeField] private GameObject[] snowmen;

    private int nextSnowmanToLoseHead = 0;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log("Snowman obstacle triggered by " + other.name);

        if (nextSnowmanToLoseHead >= snowmen.Length) return;

        Transform head = snowmen[nextSnowmanToLoseHead]
                         .transform.Find("Snowman(Head)");
        if (head == null)
        {
            // fallback: find by tag
            head = snowmen[nextSnowmanToLoseHead]
                   .GetComponentsInChildren<Transform>()
                   .FirstOrDefault(t => t.CompareTag("SnowManHead"));
        }

        if (head != null)
        {
            head.gameObject.SetActive(false);
            nextSnowmanToLoseHead++;
        }
        else
        {
            Debug.LogError("Couldn't find head on " + snowmen[nextSnowmanToLoseHead].name);
        }
    }

}