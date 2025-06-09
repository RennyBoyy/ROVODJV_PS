using UnityEngine;

public class SnowmanObstacke : MonoBehaviour
{
    [SerializeField] private GameObject[] snowmen;

    private int nextSnowmanToLoseHead = 0;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (nextSnowmanToLoseHead < snowmen.Length)
        {
            Transform head = snowmen[nextSnowmanToLoseHead].transform.Find("SnowManHead");
            if (head != null)
            {
                head.gameObject.SetActive(false);
            }
            nextSnowmanToLoseHead++;
        }
    }
}