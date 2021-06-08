using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

//handles the pause Menu of the game
public class Pause : MonoBehaviour
{
    public static bool paused=false;
    private bool disconnecting=false;

    //toggles the pause menu and makes the cursor visible
    public void TogglePause(){
        if(disconnecting){
            return;
        }
        paused=!paused;
        transform.GetChild(0).gameObject.SetActive(paused);
        Cursor.lockState=(paused)? CursorLockMode.None : CursorLockMode.Confined;
        Cursor.visible=paused;
        Debug.Log("Pause");
    }

    //function that allows the player to quit the match
    public void Quit(){
        disconnecting=true;
        PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene(0);
    }
}
