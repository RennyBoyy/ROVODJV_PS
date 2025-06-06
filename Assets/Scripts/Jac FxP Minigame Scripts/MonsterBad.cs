using UnityEngine;
using System.Collections;
using TMPro;

public class MonsterBad : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int monsterHealth = 2;
    [SerializeField] private float monsterEatingTime = 3f;

    [Header("References")]
    [SerializeField] private GameManager_Fruity gameManager;

    // State
    private bool isEating = false;
    private float eatingTimer = 0f;
    private GameObject targetLife = null;
    public bool didplayer1lose = false; 

    // Components
    private Animator _anim;
    private Rigidbody _rb;

    private void Start()
    {
        // Cache references
        _anim = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody>();

        // Find GameManager if not assigned in Inspector
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager_Fruity>();
    }

    private void Update()
    {
      

        // When eating, increment timer and destroy the life object once time is up
        if (isEating)
        {
            eatingTimer += Time.deltaTime;
            if (eatingTimer >= monsterEatingTime && targetLife != null)
            {
                // “Eat” the life: remove its collider & child visual
                var lifeCol = targetLife.GetComponent<Collider>();
                if (lifeCol != null) Destroy(lifeCol);

                if (targetLife.transform.childCount > 0)
                {
                    Destroy(targetLife.transform.GetChild(0).gameObject);
                    _anim.SetBool("MonchTime", false);
                }

                // Reset eating state
                isEating = false;
                eatingTimer = 0f;
                targetLife = null;
            }
        }

        // If health hits zero, destroy the monster
        if (monsterHealth <= 0)
        {
            Destroy(gameObject);
        }
    }

    // This is where root motion actually moves the Rigidbody
    private void OnAnimatorMove()
    {
        // If we don’t have an Animator or Rigidbody, bail
        if (_anim == null || _rb == null) return;

        if (!isEating)
        {
            Debug.Log("DeltaPosition = " + _anim.deltaPosition);
            Vector3 nextPosition = _rb.position + _anim.deltaPosition;
            _rb.MovePosition(nextPosition);

            Vector3 currentVel = _rb.linearVelocity;
            _rb.linearVelocity = new Vector3(currentVel.x, currentVel.y, currentVel.z);
        }
        else
        {
            _rb.linearVelocity = Vector3.zero;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Tomato"))
        {
            // Monster takes damage on Tomato hit
            monsterHealth--;
            Debug.Log("Monster hit by Tomato! Health now: " + monsterHealth);
        }
        else if (other.CompareTag("Lives") && !isEating)
        {
            // Found a life. Stop to eat it:
            isEating = true;
            eatingTimer = 0f;
            targetLife = other.gameObject;
            _anim.SetBool("MonchTime", true);
            Debug.Log("Monster found food, stopping to eat.");
        }
        else if (other.CompareTag("LoseCon"))
        {
            // Monster reached the “lose” trigger for player1
            gameManager.Fruit_Remaining = 0;
            didplayer1lose = true;
            if (gameManager != null)
                gameManager.TriggerGameEndFromMonster(this);
        }
        else if (other.CompareTag("LoseCon1"))
        {
            // Monster reached the “lose” trigger for player2
            gameManager.Fruit_Remaining = 0;
            didplayer1lose = false;
            if (gameManager != null)
                gameManager.TriggerGameEndFromMonster(this);
        }
    }
    
}
