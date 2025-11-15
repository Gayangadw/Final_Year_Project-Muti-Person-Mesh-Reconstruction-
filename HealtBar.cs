using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Slider healtBarSlider;
    
    public void GiveFullHealth(int health)
    {
        healtBarSlider.maxValue = health;
        healtBarSlider.value = health;
    }
    
    public void SetHealth(int health)
    {
        healtBarSlider.value = health;
    }
}