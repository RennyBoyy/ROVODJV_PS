using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager_Fruity : MonoBehaviour
{
  //  public int Stage = 0;
    public int Fruit_Remaining = 1;
    public bool gameActive = false;
    [SerializeField] private TextMeshProUGUI loseText;
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
        StartCoroutine(LoadSceneAfterDelay());
    }

    private IEnumerator LoadSceneAfterDelay()
    {
        yield return new WaitForSeconds(3f);
        SceneManager.LoadScene("RenTest");
    }
}
