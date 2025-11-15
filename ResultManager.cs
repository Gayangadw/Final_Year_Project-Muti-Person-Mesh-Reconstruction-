using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResultManager : MonoBehaviour
{
    public GameObject resultPanel;
    public Text resultText;
    public FightingCharacter[] fightingCharacter;
    public opponentAI[] OpponentAI;
    
    void Update()
    {
        foreach(FightingCharacter fightingCharacter in fightingCharacter)
        {
            if(fightingCharacter.gameObject.activeSelf && fightingCharacter.currentHealth <= 0)
            {
                SetResult("You Lose !");
                return;
            }
        }
        
        foreach(opponentAI OpponentAI in OpponentAI)
        {
            if(OpponentAI.gameObject.activeSelf && OpponentAI.currentHealth <= 0)
            {
                SetResult("You Win");
                return;
            }
        }
    }
    
    void SetResult(string result)
    {
        resultText.text = result;
        resultPanel.SetActive(true);
        Time.timeScale = 0f;
    }
    
    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}