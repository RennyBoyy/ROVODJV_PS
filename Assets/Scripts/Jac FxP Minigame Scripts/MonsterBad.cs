using UnityEngine;
using System.Collections;
using TMPro;
using UnityEditor.TerrainTools;

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
    public static bool isMoving = true;

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

        // Play spawn sound when monster is created
        FruityGameConfigurator.Instance?.PlayScarecrowSpawnSound();
    }

    private void Update()
    {
        // When eating, increment timer and destroy the life object once time is up
        if (isEating)
        {
            eatingTimer += Time.deltaTime;
            if (eatingTimer >= monsterEatingTime && targetLife != null)
            {
                // "Eat" the life: remove its collider & child visual
                var lifeCol = targetLife.GetComponent<Collider>();
                if (lifeCol != null) Destroy(lifeCol);

                if (targetLife.transform.childCount > 0)
                {
                    Destroy(targetLife.transform.GetChild(0).gameObject);
                    _anim.SetBool("MonchTime", false);

                    // Play sound when pumpkin is fully eaten
                    FruityGameConfigurator.Instance?.PlayPumpkinEatenSound();
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
            // Play death sound before destroying
            FruityGameConfigurator.Instance?.PlayScarecrowDeathSound();
            Destroy(gameObject);
        }
    }

    // This is where root motion actually moves the Rigidbody
    private void OnAnimatorMove()
    {
        if (_anim == null || _rb == null)
            return;

        // Grab this frame's root-motion delta
        Vector3 dp = _anim.deltaPosition;
        Debug.Log($"Δpos this frame: {dp.z:F3}");
        var info = _anim.GetCurrentAnimatorClipInfo(0);
        if (info.Length > 0)
            Debug.Log("Playing clip: " + info[0].clip.name);


        // Only apply it when we're allowed to move
        if (!isEating && isMoving)
        {
            _rb.MovePosition(_rb.position + dp);
        }
        else
        {
            // Cancel any horizontal drift
            _rb.linearVelocity = new Vector3(0, _rb.linearVelocity.y, 0);
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

            // Play eating sound when starting to eat
            FruityGameConfigurator.Instance?.PlayScarecrowEatingSound();
        }
        else if (other.CompareTag("LoseCon"))
        {
            // Monster reached the "lose" trigger for player1
            if (gameManager != null)
                gameManager.EndGame(0); // 0 = player1 lost
        }
        else if (other.CompareTag("LoseCon1"))
        {
            // Monster reached the "lose" trigger for player2
            if (gameManager != null)
                gameManager.EndGame(1); // 1 = player2 lost
        }
    }
}