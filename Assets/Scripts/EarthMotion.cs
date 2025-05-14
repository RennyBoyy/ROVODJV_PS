using UnityEngine;

public class EarthMotion : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Degrees per second around the up axis (standby spin)")]
    public float rotationSpeed = 10f;

    [Header("Bobbing Settings")]
    [Tooltip("How fast the bobbing cycles")]
    public float bobSpeed = 1f;
    [Tooltip("Max offset from the start position")]
    public float bobAmount = 0.1f;

    [Header("Drag Settings")]
    [Tooltip("How sensitive the drag-to-rotate is")]
    public float dragSpeed = 0.2f;

    private Vector3 startPosition;
    private bool isDragging = false;
    private Vector3 lastMousePos;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        // If the user is currently dragging, rotate based on mouse movement:
        if (isDragging)
        {
            Vector3 mouseDelta = Input.mousePosition - lastMousePos;

            // Horizontal drag ? rotate around world Y
            transform.Rotate(Vector3.up, -mouseDelta.x * dragSpeed, Space.World);
            // Vertical drag ? tilt around camera's right axis
            transform.Rotate(Camera.main.transform.right, mouseDelta.y * dragSpeed, Space.World);

            lastMousePos = Input.mousePosition;
            return; // skip standby animation while dragging
        }

        // Standby animation: continuous spin + bob
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);

        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobAmount;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
    }

    // Begin dragging when the Earth is clicked
    void OnMouseDown()
    {
        isDragging = true;
        lastMousePos = Input.mousePosition;
    }

    // Stop dragging when the mouse button is released
    void OnMouseUp()
    {
        isDragging = false;
    }
}
