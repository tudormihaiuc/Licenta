using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Hashtable=ExitGames.Client.Photon.Hashtable;
using UnityEngine.UI;

public class PlayerController : MonoBehaviourPunCallbacks, IPunObservable
{
    [SerializeField] float mouseSensitivity, sprintSpeed, walkSpeed, jumpForce, smoothTime;
    //[SerializeField] GameObject cameraHolder;

    int itemIndex;
    int previousItemIndex=-1;
    bool grounded;
    float verticalLookRotation;
    Vector3 smoothMoveVelocity;
    Vector3 moveAmount;
    Rigidbody rb;
    PhotonView photonView;

    public Transform weaponParent;
    private Vector3 weaponParentOrigin;
    private float movementCounter;
    private float idleCounter;
    private Vector3 targetWeaponBobPosition;
    public Transform player;
    public Transform cams;
    public Transform weapon;
    public float xSensitivity;
    public float ySensitivity;
    public float maxAngle;
    private Quaternion camCenter;
    public static bool cursorLocked=true;
    public int maxHealth;
    private int current_Health;
    private PlayerManager playerManager;
    private Transform uiHealthbar;
    private Weapon weaponUI;
    private Text uiAmmo;
    public float crouchModifier;
    public float crouchAmount;
    public GameObject standingCollider;
    public GameObject crouchingCollider;


    void Awake() {
        rb=GetComponent<Rigidbody>();
        photonView=GetComponent<PhotonView>();
    }

    void Start() {
        Application.targetFrameRate=50;
        QualitySettings.vSyncCount = 0;
        camCenter=cams.localRotation;
        current_Health=maxHealth;
        weaponUI=GetComponent<Weapon>();
        playerManager=GameObject.Find("PlayerManager").GetComponent<PlayerManager>();
        weaponParentOrigin=weaponParent.localPosition;
        if(photonView.IsMine){
            //EquipItem(0);//equip first item in the item array
        }else{
            Destroy(GetComponentInChildren<Camera>().gameObject);
            Destroy(rb);
            gameObject.layer=11;
        }
        if(photonView.IsMine){
            uiHealthbar=GameObject.Find("HUD/Health/Bar").transform;
            uiAmmo=GameObject.Find("HUD/Ammo/Text").GetComponent<Text>();
            RefreshHealthBar();
            }
    }

    void Update() {
        float t_hmove=Input.GetAxisRaw("Horizontal");
        float t_vmove=Input.GetAxisRaw("Vertical");
        //if i dont use this if each player would control eachother player
        if(!photonView.IsMine){
            RefreshMultiplayerState();
            return;
        }
        SetY();
        SetX();
        UpdateCursorLock();
        Move();
        Jump();
        /*for(int i=0;i<items.Length;i++){
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
        }*/
        
        if(t_hmove==0&&t_vmove==0){
            HeadBob(idleCounter,0.02f,0.02f);
            idleCounter+=Time.deltaTime;
            weaponParent.localPosition=Vector3.Lerp(weaponParent.localPosition,targetWeaponBobPosition,Time.deltaTime*2f);
        }else if(!Input.GetKey(KeyCode.LeftShift)){
            HeadBob(movementCounter,0.03f,0.03f);
            movementCounter+=Time.deltaTime*3f;
            weaponParent.localPosition=Vector3.Lerp(weaponParent.localPosition,targetWeaponBobPosition,Time.deltaTime*6f);
        }else{
            HeadBob(movementCounter,0.15f,0.075f);
            movementCounter+=Time.deltaTime*5f;
            weaponParent.localPosition=Vector3.Lerp(weaponParent.localPosition,targetWeaponBobPosition,Time.deltaTime*10f);
        }
        //test healthbar
        if(Input.GetKeyDown(KeyCode.U)){
            TakeDamage(100);
        }
        RefreshHealthBar();
        weaponUI.RefreshAmmo(uiAmmo);
    }
    //for syncing
    //we are using slerp to make estimations because when syncing we can loose some packeges and if we lose a couple is no big deal
    void RefreshMultiplayerState(){
        float cacheEulY=weaponParent.localEulerAngles.y;
        Quaternion targetRotation=Quaternion.identity*Quaternion.AngleAxis(aimAngle,Vector3.right);
        weaponParent.rotation=Quaternion.Slerp(weaponParent.rotation,targetRotation,Time.deltaTime*8f);
        Vector3 finalRotation=weaponParent.localEulerAngles;
        finalRotation.y=cacheEulY;
        weaponParent.localEulerAngles=finalRotation;
    }

    public void SetGroundedState(bool _grounded){
        grounded=_grounded;
    }
    /*void Look(){
        transform.Rotate(Vector3.up*Input.GetAxisRaw("Mouse X")*mouseSensitivity);
        verticalLookRotation+=Input.GetAxisRaw("Mouse Y")*mouseSensitivity;
        verticalLookRotation=Mathf.Clamp(verticalLookRotation,-90f,90f);
        cameraHolder.transform.localEulerAngles=Vector3.left*verticalLookRotation;
    }*/
    void Move(){
        Vector3 moveDir=new Vector3(Input.GetAxisRaw("Horizontal"),0,Input.GetAxisRaw("Vertical")).normalized;
        moveAmount=Vector3.SmoothDamp(moveAmount,moveDir*(Input.GetKey(KeyCode.LeftShift) ? sprintSpeed:walkSpeed),ref smoothMoveVelocity,smoothTime);
        
    }

    void Jump(){
        if(Input.GetKeyDown(KeyCode.Space)&&grounded){
            rb.AddForce(transform.up*jumpForce);
        }
    }

    /*void EquipItem(int _index){
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
    }*/

    public override void OnPlayerPropertiesUpdate(Player targetPlayer,Hashtable changedProps){
        //called when the info is received
        if(!photonView.IsMine && targetPlayer==photonView.Owner){
            //sync the curent item
            //EquipItem((int)changedProps["itemIndex"]);
        }
    }

    void FixedUpdate() {
        if(!photonView.IsMine)
        return;
        //we are doing the calculations here because the method Update() is called every farme 
        //and we dont want our calculations be impacted by the frame rate
        rb.MovePosition(rb.position+transform.TransformDirection(moveAmount)*Time.fixedDeltaTime);
    }

    void HeadBob(float p_z,float p_x_intensity,float p_y_intensity){
        float t_aim_adjust=1f;
        if(weaponUI.isAiming){
            t_aim_adjust=0.1f;
        }
        targetWeaponBobPosition=weaponParentOrigin + new Vector3(Mathf.Cos(p_z)*p_x_intensity*t_aim_adjust,Mathf.Sin(p_z*2)*p_y_intensity*t_aim_adjust,0);
    }
    void SetY(){
        float t_input=Input.GetAxis("Mouse Y")*ySensitivity*Time.deltaTime;
        Quaternion t_adj=Quaternion.AngleAxis(t_input,-Vector3.right);
        Quaternion t_delta=cams.localRotation*t_adj;
        if(Quaternion.Angle(camCenter,t_delta)<maxAngle){
            cams.localRotation=t_delta;
        }
        weapon.rotation=cams.rotation;
        
    }
    void SetX(){
        float t_input=Input.GetAxis("Mouse X")*xSensitivity*Time.deltaTime;
        Quaternion t_adj=Quaternion.AngleAxis(t_input,Vector3.up);
        Quaternion t_delta=player.localRotation*t_adj;
        player.localRotation=t_delta;
        
        
    }
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
    public void TakeDamage(int p_damage){
        if(photonView.IsMine){
        current_Health-=p_damage;
        Debug.Log(current_Health);
        RefreshHealthBar();
        if(current_Health<=0){
            Debug.Log("You died");
            PhotonNetwork.Destroy(gameObject);
            playerManager.CreateController();
        }
        }
    }

    void RefreshHealthBar(){
        float health_Ratio=(float)current_Health/(float)maxHealth;
        uiHealthbar.localScale=Vector3.Lerp(uiHealthbar.localScale, new Vector3(health_Ratio,1,1),Time.deltaTime*8f);
    }
    private float aimAngle;
    //receives updates frequently about stuff
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info){
        if(stream.IsWriting){
            stream.SendNext((int)(weaponParent.transform.localEulerAngles.x*100f));
        }
        else{
            aimAngle=(int)stream.ReceiveNext()/100f;
        }
    }
}
