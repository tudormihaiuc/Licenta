using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

//class that handles the free look of the player using the mouse
public class Look : MonoBehaviourPunCallbacks
{
    public Transform player;
    public Transform cams;
    public float xSensitivity;
    public float ySensitivity;
    public float maxAngle;
    private Quaternion camCenter;
    public static bool cursorLocked=true;
    // Start is called before the first frame update
    void Start()
    {
        camCenter=cams.localRotation;
    }

    // Update is called once per frame
    void Update()
    {
        if(!photonView.IsMine){
            return;
        }
        SetY();
        SetX();
        UpdateCursorLock();
        if(Pause.paused){
            return;
        }
    }

    //sets the movement on the Y axis, based on the input from the mouse
    void SetY(){
        float t_input=Input.GetAxis("Mouse Y")*ySensitivity*Time.deltaTime;
        Quaternion t_adj=Quaternion.AngleAxis(t_input,-Vector3.right);
        Quaternion t_delta=cams.localRotation*t_adj;
        if(Quaternion.Angle(camCenter,t_delta)<maxAngle){
            cams.localRotation=t_delta;
        }     
    }

    //sets the movement on the X axis, based on the input from the mouse
    void SetX(){
        float t_input=Input.GetAxis("Mouse X")*xSensitivity*Time.deltaTime;
        Quaternion t_adj=Quaternion.AngleAxis(t_input,Vector3.up);
        Quaternion t_delta=player.localRotation*t_adj;
        player.localRotation=t_delta;
        
        
    }

    //Each time the player presses the Esc key, make the mouse cursor visible/invisible 
    //(the player needs to see the cursor to navigate the Pause Menu opened also by the Esc key)
    void UpdateCursorLock(){
        if(cursorLocked){
            Cursor.lockState=CursorLockMode.Locked;
            Cursor.visible=false;
            if(Input.GetKeyDown(KeyCode.Escape)){
                cursorLocked=false;
            }
        }else{
            Cursor.lockState=CursorLockMode.None;
            Cursor.visible=true;
            if(Input.GetKeyDown(KeyCode.Escape)){
                cursorLocked=true;
            }
        }
    }
}
