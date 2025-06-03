using UnityEngine;
using TMPro;

public class GameTimer : MonoBehaviour
{
    [Header("Timer Settings")]
    [Tooltip("Starting time in seconds")]
    [SerializeField] private float startTime = 180f;
    [Tooltip("TextMeshProUGUI that displays the countdown. Place this between our split screens.")]
    [SerializeField] private TMP_Text timerText = null;

    private float _remainingTime;
    private bool _timerRunning = false;

    private void Start()
    {
        _remainingTime = startTime;
        _timerRunning = true;

        if (timerText == null)
        {
            Debug.LogError("GameTimer: timerText not assigned.");
            _timerRunning = false;
            return;
        }

        UpdateTimerDisplay();
    }

    private void Update()
    {
        if (!_timerRunning)
            return;

        _remainingTime -= Time.deltaTime;
        if (_remainingTime <= 0f)
        {
            _remainingTime = 0f;
            _timerRunning = false;
            UpdateTimerDisplay();
            OnTimerExpired();
        }
        else
        {
            UpdateTimerDisplay();
        }
    }

    private void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(_remainingTime / 60f);
        int seconds = Mathf.FloorToInt(_remainingTime % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void OnTimerExpired()
    {
        Debug.Log("Timer reached 00:00 – stopping game.");
        Time.timeScale = 0f;
    }
}
