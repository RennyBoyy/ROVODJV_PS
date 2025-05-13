using UnityEngine;

public class GameManager_Fruity : MonoBehaviour
{
    public int Stage = 0;
    public int Fruit_Remaining = 5;
    public bool gameActive = false;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Fruit_Remaining <= 0)
        {
            loseGame();
        }
    }
    private void loseGame()
    {

    }
}
