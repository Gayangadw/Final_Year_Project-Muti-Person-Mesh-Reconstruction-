using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class PlayerSelection : MonoBehaviour
{
    public GameObject playerCharacters;
    private GameObject[] allCharacters;
    private int currentIndex = 0;

    void Start()
    {
        allCharacters = new GameObject[playerCharacters.transform.childCount];

        for (int i = 0; i < playerCharacters.transform.childCount; i++)
        {
            allCharacters[i] = playerCharacters.transform.GetChild(i).gameObject;
            allCharacters[i].SetActive(false);
        }

        // ✅ Load previously selected character if available
        if (PlayerPrefs.HasKey("SelectedCharacterIndex"))
        {
            currentIndex = PlayerPrefs.GetInt("SelectedCharacterIndex");
        }

        ShowingCurrentCharacter();
    }


    void ShowingCurrentCharacter()
    {
        foreach (GameObject character in allCharacters)
        {
            character.SetActive(false);
        }
        allCharacters[currentIndex].SetActive(true);
    }

    public void NextCharacter()
    {
        currentIndex = (currentIndex + 1) % allCharacters.Length;
        ShowingCurrentCharacter();
    }

    public void PreviousCharacter()
    {
        currentIndex = (currentIndex - 1 + allCharacters.Length) % allCharacters.Length;
        ShowingCurrentCharacter();
    }

    public void OnYesButtonClick(string sceneName)
    {
        PlayerPrefs.SetInt("SelectedCharacterIndex", currentIndex);
        PlayerPrefs.Save();

        SceneManager.LoadScene(sceneName);
    }
}
