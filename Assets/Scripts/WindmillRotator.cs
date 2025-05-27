using UnityEngine;

public class WindmillRotator : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 30f;
    [SerializeField] private bool rotateClockwise = true;

    void Update()
    {
        float rotationAmount = rotationSpeed * Time.deltaTime;

        if (!rotateClockwise)
        {
            rotationAmount = -rotationAmount;
        }

        transform.Rotate(rotationAmount, 0f, 0f, Space.Self);
    }
}