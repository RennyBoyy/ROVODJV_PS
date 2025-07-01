using UnityEngine;
using UnityEngine.UI;

public class SkiProgressTracker : MonoBehaviour
{
    [Header("Track References")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform finishPoint;

    [Header("Player References")]
    [SerializeField] private Transform player1Transform;
    [SerializeField] private Transform player2Transform;

    [Header("UI Elements")]
    [SerializeField] private RectTransform progressBar;
    [SerializeField] private RectTransform player1Icon;
    [SerializeField] private RectTransform player2Icon;

    [Header("Settings")]
    [SerializeField] private float player1IconOffset = 15f;      
    [SerializeField] private float player2IconOffset = -15f;     

    private float trackLength;
    private float startZ;
    private float finishZ;
    private float progressBarWidth;

    void Start()
    {
        InitializeTracker();
    }

    void InitializeTracker()
    {
        if (startPoint == null || finishPoint == null)
        {
            Debug.LogError("SkiProgressTracker: Start point and finish point must be assigned!");
            return;
        }

        startZ = startPoint.position.z;
        finishZ = finishPoint.position.z;
        trackLength = Mathf.Abs(finishZ - startZ);

        if (progressBar != null)
        {
            progressBarWidth = progressBar.rect.width;
        }

        Debug.Log($"Track initialized: Start Z={startZ}, Finish Z={finishZ}, Length={trackLength}");
    }

    void Update()
    {
        UpdatePlayerProgress();
    }

    void UpdatePlayerProgress()
    {
        if (progressBar == null || trackLength <= 0) return;

        if (player1Transform != null && player1Icon != null)
        {
            float player1Progress = CalculateProgress(player1Transform.position.z);
            UpdateIconPosition(player1Icon, player1Progress, player1IconOffset);
        }

        if (player2Transform != null && player2Icon != null)
        {
            float player2Progress = CalculateProgress(player2Transform.position.z);
            UpdateIconPosition(player2Icon, player2Progress, player2IconOffset);
        }
    }

    float CalculateProgress(float currentZ)
    {
        float progress;

        if (finishZ > startZ)
        {
            progress = (currentZ - startZ) / trackLength;
        }
        else
        {
            progress = (startZ - currentZ) / trackLength;
        }

        return Mathf.Clamp01(progress);
    }

    void UpdateIconPosition(RectTransform icon, float progress, float yOffset)
    {
        if (icon == null || progressBar == null) return;

        float xPosition = (progress * progressBarWidth) - (progressBarWidth * 0.5f);

        Vector2 newPosition = new Vector2(xPosition, yOffset);
        icon.anchoredPosition = newPosition;
    }

    public float GetPlayerProgress(int playerID)
    {
        if (playerID == 1 && player1Transform != null)
        {
            return CalculateProgress(player1Transform.position.z);
        }
        else if (playerID == 2 && player2Transform != null)
        {
            return CalculateProgress(player2Transform.position.z);
        }

        return 0f;
    }

    public bool HasPlayerFinished(int playerID)
    {
        return GetPlayerProgress(playerID) >= 1f;
    }

    public int GetLeadingPlayer()
    {
        float player1Progress = GetPlayerProgress(1);
        float player2Progress = GetPlayerProgress(2);

        if (player1Progress > player2Progress)
            return 1;
        else if (player2Progress > player1Progress)
            return 2;
        else
            return 0;
    }

    public void SetPlayerReferences(Transform p1, Transform p2)
    {
        player1Transform = p1;
        player2Transform = p2;
    }

    public void SetTrackPoints(Transform start, Transform finish)
    {
        startPoint = start;
        finishPoint = finish;
        InitializeTracker();
    }

    public void SetPlayerOffsets(float player1Offset, float player2Offset)
    {
        player1IconOffset = player1Offset;
        player2IconOffset = player2Offset;
    }
}