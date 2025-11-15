using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class StageSelect : MonoBehaviour
{
   public void SelectStage(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    } 
}
