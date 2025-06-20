using System.Collections;
using UnityEngine;

public class FinishLineCameraController : MonoBehaviour
{
    [Header("Camera References")]
    [SerializeField] private Camera cinematicCamera;
    [SerializeField] private Camera player1Camera;
    [SerializeField] private Camera player2Camera;

    [Header("Settings")]
    [SerializeField] private float cinematicDuration = 3f;

    private bool cinematicTriggered = false;
    private bool gameFinished = false;

    void Start()
    {
        if (cinematicCamera != null)
            cinematicCamera.gameObject.SetActive(false);

        ValidateSetup();
    }

    void ValidateSetup()
    {
        if (cinematicCamera == null)
            Debug.LogWarning("FinishLineCameraController: Cinematic Camera not assigned!");

        if (player1Camera == null)
            Debug.LogWarning("FinishLineCameraController: Player 1 Camera not assigned!");

        if (player2Camera == null)
            Debug.LogWarning("FinishLineCameraController: Player 2 Camera not assigned!");
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !cinematicTriggered)
        {
            TriggerCinematicCamera();
        }
    }

    void TriggerCinematicCamera()
    {
        if (cinematicTriggered) return;

        cinematicTriggered = true;
        Debug.Log("FinishLineCameraController: Switching to cinematic camera!");

        StartCoroutine(HandleCinematicSequence());
    }

    IEnumerator HandleCinematicSequence()
    {
        SwitchCameras(true);

        yield return new WaitForSeconds(cinematicDuration);

        gameFinished = true;
        Debug.Log("FinishLineCameraController: Cinematic finished, ready for podium logic");

    }

    void SwitchCameras(bool useCinematic)
    {
        if (useCinematic)
        {
            if (cinematicCamera != null)
                cinematicCamera.gameObject.SetActive(true);

            if (player1Camera != null)
                player1Camera.gameObject.SetActive(false);
            if (player2Camera != null)
                player2Camera.gameObject.SetActive(false);
        }
        else
        {
            if (cinematicCamera != null)
                cinematicCamera.gameObject.SetActive(false);

            if (player1Camera != null)
                player1Camera.gameObject.SetActive(true);
            if (player2Camera != null)
                player2Camera.gameObject.SetActive(true);
        }
    }

    public void ForceCinematicCamera()
    {
        if (!cinematicTriggered)
        {
            TriggerCinematicCamera();
        }
    }

    [ContextMenu("Test Cinematic Camera")]
    public void TestCinematicCamera()
    {
        if (!cinematicTriggered)
        {
            TriggerCinematicCamera();
        }
    }

    [ContextMenu("Reset Camera State")]
    public void ResetCameraState()
    {
        cinematicTriggered = false;
        gameFinished = false;
        SwitchCameras(false);
        Debug.Log("FinishLineCameraController: Camera state reset");
    }

    public bool IsCinematicComplete => gameFinished;
}