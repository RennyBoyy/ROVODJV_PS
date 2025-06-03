using UnityEngine;
using System.Collections;
using TMPro;

public class MonsterBad : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private int monsterHealth = 2;
    [SerializeField] private GameManager_Fruity gameManager;
    [SerializeField] private float monsterEatingTime = 3f;
    private bool isEating = false;
    private float eatingTimer = 0f;
    private GameObject targetLife;
    public bool didplayer1lose = false; // true if player 1 lost, false if player 2 lost

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
                        if (targetLife.transform.childCount > 0)
                        {
                            Transform child = targetLife.transform.GetChild(0);
                            Destroy(child.gameObject);
                        }
                    }


                    isEating = false;
                    eatingTimer = 0f;
                }
            }
        }
        if (monsterHealth <= 0)
        {
            Destroy(this.gameObject);
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
            gameManager.Fruit_Remaining = 0;
            didplayer1lose = true;
            if (gameManager != null)
                gameManager.TriggerGameEndFromMonster(this);
        }
        else if (other.gameObject.CompareTag("LoseCon1"))
        {
            gameManager.Fruit_Remaining = 0;
            didplayer1lose = false;
            if (gameManager != null)
                gameManager.TriggerGameEndFromMonster(this);
        }
    }
}