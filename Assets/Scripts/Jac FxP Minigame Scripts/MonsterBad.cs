using UnityEngine;

public class MonsterBad : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int monsterHealth = 2;
    [SerializeField] private float monsterEatingTime = 3f;
    [SerializeField] private string deathAnimationTrigger = "Death";
    [SerializeField] private float deathAnimationDuration = 1.5f;

    [Header("References")]
    [SerializeField] private GameManager_Fruity gameManager;

    // State
    private bool isEating = false;
    private bool isDying = false;
    public bool isMoving = true;
    private float eatingTimer = 0f;
    private float deathTimer = 0f;
    private GameObject targetLife = null;
    public bool didplayer1lose = false;

    // Components
    private Animator _anim;
    private Rigidbody _rb;

    // Static method to stop all monsters (for game end)
    public static void StopAllMonsters()
    {
        MonsterBad[] monsters = FindObjectsByType<MonsterBad>(FindObjectsSortMode.None);
        foreach (var monster in monsters)
        {
            if (monster != null)
                monster.isMoving = false;
        }
    }

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
        // Handle death animation sequence
        if (isDying)
        {
            deathTimer += Time.deltaTime;
            if (deathTimer >= deathAnimationDuration)
            {
                // Death animation finished, destroy the monster
                Destroy(gameObject);
            }
            return; // Don't process other updates while dying
        }

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

        // If health hits zero, start death sequence
        if (monsterHealth <= 0 && !isDying)
        {
            StartDeathSequence();
        }
    }

    private void StartDeathSequence()
    {
        isDying = true;
        deathTimer = 0f;
        
        // Disable movement and other behaviors
        isMoving = false;
        isEating = false;
        
        // Disable collider to prevent further interactions
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        
        // Play death animation
        if (_anim != null)
        {
            _anim.SetTrigger(deathAnimationTrigger);
        }
        
        // Play death sound
        FruityGameConfigurator.Instance?.PlayScarecrowDeathSound();
        
        Debug.Log("Monster started death sequence");
    }

    // This is where root motion actually moves the Rigidbody
    private void OnAnimatorMove()
    {
        if (_anim == null || _rb == null)
            return;

        // Don't move during death sequence
        if (isDying)
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
        if (isDying) return;

        if (other.CompareTag("Tomato"))
        {
            monsterHealth--;
            Debug.Log("Monster hit by Tomato! Health now: " + monsterHealth);
        }
        else if (other.CompareTag("Lives") && !isEating)
        {
            isEating = true;
            eatingTimer = 0f;
            targetLife = other.gameObject;
            _anim.SetBool("MonchTime", true);
            FruityGameConfigurator.Instance?.PlayScarecrowEatingSound();
        }
        else if (other.TryGetComponent<LoseTrigger>(out var loseTrigger))
        {
            if (gameManager != null)
            {
                int losingPlayer = loseTrigger.playerToLose == PlayerIdentity.Fruity ? 0 : 1;
                gameManager.EndGame(losingPlayer);
            }
        }
    }
}