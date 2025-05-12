using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float projectileSpeed = 10f;
   
    void Update()
    {
     this.transform.position += transform.forward * projectileSpeed * Time.deltaTime;
        
    }
}
