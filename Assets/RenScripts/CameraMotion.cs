using UnityEngine;
using System.Collections;

public class CameraMotion : MonoBehaviour
{
    [Header("Targets & Offsets")]
    [Tooltip("The Earth transform to zoom into")]
    public Transform target;

    [Tooltip("Camera position relative to target at start")]
    public Vector3 startOffset = new Vector3(0f, 20f, -50f);

    [Tooltip("Camera position relative to target at end")]
    public Vector3 endOffset = new Vector3(0f, 5f, -15f);

    [Header("Timing")]
    [Tooltip("Seconds it takes to complete the zoom")]
    public float zoomDuration = 2f;

    private float t = 0f;

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("CameraMotion: No target assigned!");
            enabled = false;
            return;
        }

        // Place camera at the far offset and look at the Earth
        transform.position = target.position + startOffset;
        transform.LookAt(target);

        // Begin the zoom coroutine
        StartCoroutine(DoZoom());
    }

    IEnumerator DoZoom()
    {
        while (t < 1f)
        {
            t += Time.deltaTime / zoomDuration;

            // Lerp offset and update position
            Vector3 currentOffset = Vector3.Lerp(startOffset, endOffset, t);
            transform.position = target.position + currentOffset;

            // Always look at the Earth
            transform.LookAt(target);

            yield return null;
        }
    }
}
