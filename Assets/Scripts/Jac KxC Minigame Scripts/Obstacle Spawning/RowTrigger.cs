using UnityEngine;

public class RowTrigger : MonoBehaviour
{
    private int rowIndex;
    private bool hasTriggered = false;

    public void Initialize(int row)
    {
        rowIndex = row;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || hasTriggered) return;

        hasTriggered = true;
        GetComponent<Collider>().enabled = false;
        NotifyComplexObstacles(other.gameObject);
    }

    private void NotifyComplexObstacles(GameObject triggeringPlayer)
    {
        FallingTreeObstacle[] trees = FindObjectsByType<FallingTreeObstacle>(FindObjectsSortMode.None);
        foreach (FallingTreeObstacle tree in trees)
        {
            tree.OnRowTriggered(rowIndex, triggeringPlayer);
        }

        IceArchObstacle[] iceArches = FindObjectsByType<IceArchObstacle>(FindObjectsSortMode.None);
        foreach (IceArchObstacle iceArch in iceArches)
        {
            iceArch.OnRowTriggered(rowIndex, triggeringPlayer);
        }

        PenguinMeteorObstacle[] penguins = FindObjectsByType<PenguinMeteorObstacle>(FindObjectsSortMode.None);
        foreach (PenguinMeteorObstacle penguin in penguins)
        {
            penguin.OnRowTriggered(rowIndex, triggeringPlayer);
        }
    }
}