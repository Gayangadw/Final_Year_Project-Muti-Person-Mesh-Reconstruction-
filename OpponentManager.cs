using UnityEngine;

public class OpponentManager : MonoBehaviour
{
    public GameObject[] opponentCharacters;

    void Start()
    {
        if (opponentCharacters.Length == 0)
        {
            Debug.LogError("No opponent assigned to the OpponentManager!");
            return;
        }

        int selectedOpponent = 0;

        if (PlayerPrefs.HasKey("SelectedOpponentIndex"))
        {
            selectedOpponent = PlayerPrefs.GetInt("SelectedOpponentIndex");
        }

        ActivateOpponent(selectedOpponent);
    }

    void ActivateOpponent(int index)
    {
        for (int i = 0; i < opponentCharacters.Length; i++)
        {
            opponentCharacters[i].SetActive(i == index);
        }
    }
}
