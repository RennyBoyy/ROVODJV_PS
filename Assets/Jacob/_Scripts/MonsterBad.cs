using UnityEngine;
using System.Collections;
using TMPro;

public class MonsterBad : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private int monsterHealth = 3;
    [SerializeField] private GameManager_Fruity gameManager;
    [SerializeField] private float monsterEatingTime = 3f;
    private bool isEating = false;
    private float eatingTimer = 0f;
    private GameObject targetLife;

    private void Start()
    {
        gameManager = FindFirstObjectByType<GameManager_Fruity>();
    }

    void Update()
    {
        if (!isEating)
        {
            this.transform.position += -transform.forward * moveSpeed * Time.deltaTime;
        }
        else
        {
            eatingTimer += Time.deltaTime;
            if (eatingTimer >= monsterEatingTime)
            {
                if (targetLife != null)
                {
                    Debug.Log("Life lost");
                    if (targetLife != null)
                    {
                        Debug.Log("Life lost");
                        var collider = targetLife.GetComponent<Collider>();
                        if (collider != null)
                        {
                            Destroy(collider);
                        }
                    }


                    isEating = false;
                    eatingTimer = 0f;
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Tomato"))
        {
            Debug.Log("hit Tomato");
            monsterHealth--;
        }
        else if (other.gameObject.CompareTag("Lives") && !isEating)
        {
            Debug.Log("Monster found food, stopping to eat");
            isEating = true;
            eatingTimer = 0f;
            targetLife = other.gameObject;
        }
        else if (other.gameObject.CompareTag("LoseCon"))
        {
            Debug.Log("You lose!");
            gameManager.Fruit_Remaining = 0;
        }
    }
}