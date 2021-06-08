using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//handles opening/cosing of the different tabs of the menu
public class Menu : MonoBehaviour
{
public string menuName;
 public bool open;

    public void Open(){
        open=true;
        gameObject.SetActive(true);
    }

    public void Close(){
        open=false;
        gameObject.SetActive(false);
    }

    //when the script runs, makes sure that the cursor is visible
    private void Start() {
        Pause.paused=false;
        Cursor.lockState=CursorLockMode.None;
        Cursor.visible=true;
    }
}
