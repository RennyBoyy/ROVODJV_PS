using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float deathTime = 4f;
    public float projectileSpeed = 10f;

    void Start()
    {
        Destroy(this.gameObject, deathTime);
    }

    void Update()
    {
        transform.position += -transform.forward * projectileSpeed * Time.deltaTime;
    }
}
