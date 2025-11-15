using UnityEngine;

public class CharacterLoader : MonoBehaviour
{
    public GameObject playerCharacters;    // Assign in Inspector (same prefab list used in selection scene)
    public GameObject opponentCharacters;  // Assign in Inspector (same prefab list used in opponent selection)

    private GameObject playerInstance;
    private GameObject opponentInstance;

    void Start()
    {
        // Load saved indices
        int playerIndex = PlayerPrefs.GetInt("SelectedCharacterIndex", 0);
        int opponentIndex = PlayerPrefs.GetInt("SelectedOpponentIndex", 0);

        // Disable all children initially
        foreach (Transform child in playerCharacters.transform)
            child.gameObject.SetActive(false);

        foreach (Transform child in opponentCharacters.transform)
            child.gameObject.SetActive(false);

        // Activate the selected ones
        playerInstance = playerCharacters.transform.GetChild(playerIndex).gameObject;
        opponentInstance = opponentCharacters.transform.GetChild(opponentIndex).gameObject;

        playerInstance.SetActive(true);
        opponentInstance.SetActive(true);
    }
}
