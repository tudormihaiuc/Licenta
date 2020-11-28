using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Hashtable=ExitGames.Client.Photon.Hashtable;

public class PlayerController : MonoBehaviourPunCallbacks
{
    [SerializeField] float mouseSensitivity, sprintSpeed, walkSpeed, jumpForce, smoothTime;
    [SerializeField] GameObject cameraHolder;

    [SerializeField] Item[] items;

    int itemIndex;
    int previousItemIndex=-1;
    bool grounded;
    float verticalLookRotation;
    Vector3 smoothMoveVelocity;
    Vector3 moveAmount;
    Rigidbody rb;
    PhotonView PV;

    void Awake() {
        rb=GetComponent<Rigidbody>();
        PV=GetComponent<PhotonView>();
    }

    void Start() {
        Application.targetFrameRate=50;
        if(PV.IsMine){
            EquipItem(0);//equip first item in the item array
        }else{
            Destroy(GetComponentInChildren<Camera>().gameObject);
            Destroy(rb);
        }
    }

    void Update() {
        //if i dont use this if each player would control eachother player
        if(!PV.IsMine)
        return;
        Look();
        Move();
        Jump();
        for(int i=0;i<items.Length;i++){
            if(Input.GetKeyDown((i+1).ToString())){
                EquipItem(i);
                break;
            }
        }
        if(Input.GetAxisRaw("Mouse ScrollWheel")>0f){
            if(itemIndex>=items.Length-1){
                EquipItem(0);
            }else{
                EquipItem(itemIndex+1);
                }
        }
        if(Input.GetAxisRaw("Mouse ScrollWheel")<0f){
            if(itemIndex<=0){
                EquipItem(items.Length-1);
            }else{
                EquipItem(itemIndex-1);
                }
        }
    }

    public void SetGroundedState(bool _grounded){
        grounded=_grounded;
    }
    void Look(){
        transform.Rotate(Vector3.up*Input.GetAxisRaw("Mouse X")*mouseSensitivity);
        verticalLookRotation+=Input.GetAxisRaw("Mouse Y")*mouseSensitivity;
        verticalLookRotation=Mathf.Clamp(verticalLookRotation,-90f,90f);
        cameraHolder.transform.localEulerAngles=Vector3.left*verticalLookRotation;
    }
    void Move(){
        Vector3 moveDir=new Vector3(Input.GetAxisRaw("Horizontal"),0,Input.GetAxisRaw("Vertical")).normalized;
        moveAmount=Vector3.SmoothDamp(moveAmount,moveDir*(Input.GetKey(KeyCode.LeftShift) ? sprintSpeed:walkSpeed),ref smoothMoveVelocity,smoothTime);
        
    }

    void Jump(){
        if(Input.GetKeyDown(KeyCode.Space)&&grounded){
            rb.AddForce(transform.up*jumpForce);
        }
    }

    void EquipItem(int _index){
        if(_index==previousItemIndex){
            return;
        }
        itemIndex=_index;
        items[itemIndex].itemGameObject.SetActive(true);
        if(previousItemIndex!=-1){
            items[previousItemIndex].itemGameObject.SetActive(false);
        }
        previousItemIndex=itemIndex;
        if(PV.IsMine){//check if is the local player
            //send the item index over the network
            Hashtable hash=new Hashtable();
            hash.Add("itemIndex",itemIndex);
            PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer,Hashtable changedProps){
        //called when the info is received
        if(!PV.IsMine && targetPlayer==PV.Owner){
            //sync the curent item
            EquipItem((int)changedProps["itemIndex"]);
        }
    }

    void FixedUpdate() {
        if(!PV.IsMine)
        return;
        //we are doing the calculations here because the method Update() is called every farme 
        //and we dont want our calculations be impacted by the frame rate
        rb.MovePosition(rb.position+transform.TransformDirection(moveAmount)*Time.fixedDeltaTime);
    }
    
}
