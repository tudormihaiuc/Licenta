using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//makes quitting the app possible
public class QuitGame : MonoBehaviour
{
    public void Quit(){
        Debug.Log("quit application");
        Application.Quit();
    }
}
