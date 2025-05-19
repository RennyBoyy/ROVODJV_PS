using TMPro;
using UnityEngine;

public class GameManager_Fruity : MonoBehaviour
{
    public int Stage = 0;
    public int Fruit_Remaining = 1;
    public bool gameActive = false;
    [SerializeField] private TextMeshProUGUI loseText;
    [SerializeField] private bool Player1;
    [SerializeField] private bool Player2;
    void Start()
    {
        
    }

    void Update()
    {
        if (Fruit_Remaining <= 0)
        {
            loseGame();
        }
    }
    private void loseGame()
    {
        Debug.Log("You lose!");
        loseText.gameObject.SetActive(true);
    }
}
