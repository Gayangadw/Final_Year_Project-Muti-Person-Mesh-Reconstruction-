using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
   public GameObject mainMenu;
   public GameObject selectCharacterAndStageMenu;
   public GameObject optionMenu;
   public GameObject controlsMenu;
   


   void Start()
   {
	   mainMenu.SetActive(true);
	   selectCharacterAndStageMenu.SetActive(false);
	   optionMenu.SetActive(false);
	   controlsMenu.SetActive(false);
   }

   public void PlayButtonClicked()
   {
	   mainMenu.SetActive(false);
	   selectCharacterAndStageMenu.SetActive(true);
   }

   public void OptionsButtonClicked()
   {
	   mainMenu.SetActive(false);
	   optionMenu.SetActive(true);
   }

   public void ControlsButtonClicked()
   {
	   optionMenu.SetActive(false);
	   controlsMenu.SetActive(true);
   }

   public void ExitButtonClicked()
   {
	   Application.Quit();
   }

   public void BackButtonClicked()
   {
	   mainMenu.SetActive(true);
	   selectCharacterAndStageMenu.SetActive(false);
	   optionMenu.SetActive(false);
	   controlsMenu.SetActive(false);
   }

   public void SelectCharacterClicked(string SceneName)
   {
	   SceneManager.LoadScene(SceneName);
   }

   public void SelectOpponentCharacterClicked(string SceneName)
   {
       SceneManager.LoadScene(SceneName);
   }



    public void SelectStageClicked(string SceneName)
   {
	   SceneManager.LoadScene(SceneName);
	   Time.timeScale = 1f;
   }
}
