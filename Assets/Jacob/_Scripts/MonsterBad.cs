using Unity.VisualScripting;
using UnityEngine;

public class MonsterBad : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private int monsterHealth = 3;
    void Update()
    {
      this.transform.position += -transform.forward * moveSpeed * Time.deltaTime;  
        if (monsterHealth <= 0)
        {
            Destroy(this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Tomato"))
        {
            Debug.Log("hit Tomato");
            monsterHealth--;
        }
    }
}
