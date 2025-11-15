using UnityEngine;
using UnityEngine.SceneManagement;

public class OpponentSelection : MonoBehaviour
{
    public GameObject opponentCharacters;
    private GameObject[] allOpponents;
    private int currentIndex = 0;

    void Start()
    {
        allOpponents = new GameObject[opponentCharacters.transform.childCount];

        for (int i = 0; i < opponentCharacters.transform.childCount; i++)
        {
            allOpponents[i] = opponentCharacters.transform.GetChild(i).gameObject;
            allOpponents[i].SetActive(false);
        }

        if (PlayerPrefs.HasKey("SelectedOpponentIndex"))
        {
            currentIndex = PlayerPrefs.GetInt("SelectedOpponentIndex");
        }

        ShowCurrentOpponent();
    }

    void ShowCurrentOpponent()
    {
        foreach (GameObject opponent in allOpponents)
        {
            opponent.SetActive(false);
        }
        allOpponents[currentIndex].SetActive(true);
    }

    public void NextOpponent()
    {
        currentIndex = (currentIndex + 1) % allOpponents.Length;
        ShowCurrentOpponent();
    }

    public void PreviousOpponent()
    {
        currentIndex = (currentIndex - 1 + allOpponents.Length) % allOpponents.Length;
        ShowCurrentOpponent();
    }

    public void OnConfirmButtonClick(string sceneName)
    {
        PlayerPrefs.SetInt("SelectedOpponentIndex", currentIndex);
        PlayerPrefs.Save();

        SceneManager.LoadScene(sceneName);
    }
}
