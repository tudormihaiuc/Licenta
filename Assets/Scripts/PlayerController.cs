using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine.UI;

public class PlayerController : MonoBehaviourPunCallbacks, IPunObservable
{
    [SerializeField] float mouseSensitivity, sprintModifier, walkSpeed, jumpForce, smoothTime;
    //[SerializeField] GameObject cameraHolder;
    //walkSpeed=speed, sprintSpeed=sprintModifier;
    //slideSpeed=slideModifier
    int itemIndex;
    int previousItemIndex = -1;
    bool grounded;
    float verticalLookRotation;
    Vector3 smoothMoveVelocity;
    Vector3 moveAmount;
    Rigidbody rb;
    PhotonView photonView;

    public Transform weaponParent;
    private Vector3 weaponParentOrigin;
    private Vector3 weaponParentCurrPos;
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
    public static bool cursorLocked = true;
    public int maxHealth;
    private int currentHealth;
    private PlayerManager playerManager;
    private Transform uiHealthbar;
    private Weapon weaponUI;
    private Text uiAmmo;
    public float crouchModifier;
    public float crouchAmount;//how much the camera moves when crouching
    public float slideAmount;//how much the camera moves when sliding
    public GameObject standingCollider;
    public GameObject mesh;
    public GameObject crouchingCollider;
    private bool sliding;
    private float slideTime;
    public float slideLenght;
    private Vector3 slideDir;
    public float slideModifier;
    public Camera normalCam;
    public Camera weaponCam;
    private float baseFOV;
    private float sprintFOVModifier = 1.2f;
    private Vector3 origin;
    private bool crouched;

    private bool isAiming;

    //jetpack
    public float jetForce;
    public float jetWait;
    public float maxFuel;
    private float currentFuel;
    private float currentRecovery;
    public float jetRecovery;
    private Transform uiFuelbar;
    private bool canJet;
    private Animator animator;





    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        photonView = GetComponent<PhotonView>();
    }

    void Start()
    {
        Application.targetFrameRate = 50;
        QualitySettings.vSyncCount = 0;
        camCenter = cams.localRotation;
        currentHealth = maxHealth;
        baseFOV = normalCam.fieldOfView;
        origin = normalCam.transform.localPosition;
        weaponUI = GetComponent<Weapon>();
        playerManager = GameObject.Find("PlayerManager").GetComponent<PlayerManager>();
        weaponParentOrigin = weaponParent.localPosition;
        weaponParentCurrPos = weaponParentOrigin;
        currentFuel = maxFuel;
        if (photonView.IsMine)
        {
            uiHealthbar = GameObject.Find("HUD/Health/Bar").transform;
            uiAmmo = GameObject.Find("HUD/Ammo/Text").GetComponent<Text>();
            RefreshHealthBar();
            uiFuelbar = GameObject.Find("HUD/Fuel/Bar").transform;
            animator=GetComponent<Animator>();
            //EquipItem(0);//equip first item in the item array
        }
        else
        {
            Destroy(GetComponentInChildren<Camera>().gameObject);
            Destroy(rb);
            gameObject.layer = 11;
            standingCollider.layer = 11;
            crouchingCollider.layer = 11;
            ChangeLayer(mesh.transform,11);
        }
    }

    private void ChangeLayer(Transform p_transform,int p_layer){
        p_transform.gameObject.layer=p_layer;
        foreach(Transform t in p_transform){
            ChangeLayer(t,p_layer);
        }
    }
    void Update()
    {
        //if i dont use this if each player would control eachother player
        if (!photonView.IsMine)
        {
            RefreshMultiplayerState();
            return;
        }
        float t_hmove = Input.GetAxisRaw("Horizontal");
        float t_vmove = Input.GetAxisRaw("Vertical");
        bool jump = Input.GetKeyDown(KeyCode.Space);
        bool sprint = Input.GetKey(KeyCode.LeftShift);
        bool crouch = Input.GetKeyDown(KeyCode.LeftControl);
        bool pause = Input.GetKeyDown(KeyCode.Escape);
        bool isJumping = jump && grounded;
        bool isSprinting = sprint && !isJumping && grounded && t_vmove > 0;
        bool isCrouching = crouch && !isSprinting && grounded && !isJumping;

        if (pause)
        {
            GameObject.Find("Pause").GetComponent<Pause>().TogglePause();
        }
        if (Pause.paused)
        {
            t_hmove = 0f;
            t_vmove = 0f;
            jump = false;
            crouch = false;
            sprint = false;
            pause = false;
            isJumping = false;
            isCrouching = false;
            isSprinting = false;
            grounded = false;
        }

        SetY();
        SetX();
        //UpdateCursorLock();
        //Move();
        //Jump();
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
        if (isCrouching)
        {
            photonView.RPC("SetCrouch", RpcTarget.All, !crouched);
        }
        if (isJumping)
        {
            if (crouched)
            {
                photonView.RPC("SetCrouch", RpcTarget.All, false);
            }
            rb.AddForce(Vector3.up * jumpForce);
            currentRecovery = 0f;
        }
        //HeadBob
        if (!grounded)//flying
        {
           HeadBob(idleCounter, 0.02f, 0.02f);
            weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 2f);
        }//sliding
        else if (sliding)
        {
            HeadBob(movementCounter, 0.15f, 0.075f);
            weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 10f);
        }//idle
        else if (t_hmove == 0 && t_vmove == 0)
        {
            HeadBob(idleCounter, 0.02f, 0.02f);
            idleCounter += Time.deltaTime;
            weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 2f);
        }
        else if (!Input.GetKey(KeyCode.LeftShift) && !crouched)
        {//walking
            HeadBob(movementCounter, 0.03f, 0.03f);
            movementCounter += Time.deltaTime * 3f;
            weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 6f);
        }//crouching
        else if (crouched)
        {
            HeadBob(movementCounter, 0.02f, 0.02f);
            movementCounter += Time.deltaTime * 1.75f;
            weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 6f);
        }
        else
        {//sprinting
            HeadBob(movementCounter, 0.15f, 0.075f);
            movementCounter += Time.deltaTime * 5f;
            weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 10f);
        }

        //test healthbar
        if (Input.GetKeyDown(KeyCode.U))
        {
            TakeDamage(100);
        }
        RefreshHealthBar();
        if (photonView.IsMine)
            weaponUI.RefreshAmmo(uiAmmo);
    }
    //for syncing
    //we are using slerp to make estimations because when syncing we can loose some packeges and if we lose a couple is no big deal
    void RefreshMultiplayerState()
    {
        float cacheEulY = weaponParent.localEulerAngles.y;
        Quaternion targetRotation = Quaternion.identity * Quaternion.AngleAxis(aimAngle, Vector3.right);
        weaponParent.rotation = Quaternion.Slerp(weaponParent.rotation, targetRotation, Time.deltaTime * 8f);
        Vector3 finalRotation = weaponParent.localEulerAngles;
        finalRotation.y = cacheEulY;
        weaponParent.localEulerAngles = finalRotation;
    }

    public void SetGroundedState(bool _grounded)
    {
        grounded = _grounded;
    }
    /*void Look(){
        transform.Rotate(Vector3.up*Input.GetAxisRaw("Mouse X")*mouseSensitivity);
        verticalLookRotation+=Input.GetAxisRaw("Mouse Y")*mouseSensitivity;
        verticalLookRotation=Mathf.Clamp(verticalLookRotation,-90f,90f);
        cameraHolder.transform.localEulerAngles=Vector3.left*verticalLookRotation;
    }*/
    void Move()
    {
        Vector3 moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
        moveAmount = Vector3.SmoothDamp(moveAmount, moveDir * (Input.GetKey(KeyCode.LeftShift) ? sprintModifier : walkSpeed), ref smoothMoveVelocity, smoothTime);

    }

    void Jump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && grounded)
        {
            rb.AddForce(transform.up * jumpForce);
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

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        //called when the info is received
        if (!photonView.IsMine && targetPlayer == photonView.Owner)
        {
            //sync the curent item
            //EquipItem((int)changedProps["itemIndex"]);
        }
    }

    void FixedUpdate()
    {
        if (!photonView.IsMine)
            return;
        float t_hmove = Input.GetAxisRaw("Horizontal");
        float t_vmove = Input.GetAxisRaw("Vertical");
        bool jump = Input.GetKeyDown(KeyCode.Space);
        bool sprint = Input.GetKey(KeyCode.LeftShift);
        bool slide = Input.GetKey(KeyCode.LeftControl);
        bool aim = Input.GetMouseButton(1);
        bool jet = Input.GetKey(KeyCode.Space);
        bool isJumping = jump && grounded;
        bool isSprinting = sprint && !isJumping && grounded && t_vmove > 0;
        bool isSliding = isSprinting && slide && !sliding;
        isAiming = aim && !isSliding && !isSprinting;
        //we are doing the calculations here because the method Update() is called every farme 
        //and we dont want our calculations be impacted by the frame rate
        //rb.MovePosition(rb.position+transform.TransformDirection(moveAmount)*Time.fixedDeltaTime);
        if (Pause.paused)
        {
            t_hmove = 0f;
            t_vmove = 0f;
            jump = false;
            sprint = false;
            isJumping = false;
            isSprinting = false;
            grounded = false;
            isSliding = false;
            isAiming = false;
        }
        //Movement
        Vector3 t_direction = Vector3.zero;
        float t_adjustedSpeed = walkSpeed;
        if (!sliding)
        {
            t_direction = new Vector3(t_hmove, 0, t_vmove);
            t_direction.Normalize();
            t_direction = transform.TransformDirection(t_direction);
            if (isSprinting)
            {
                if (crouched)
                {
                    photonView.RPC("SetCrouch", RpcTarget.All, false);
                }
                t_adjustedSpeed *= sprintModifier;
            }
            else if (crouched)
            {
                t_adjustedSpeed *= crouchModifier;
            }
        }
        else
        {
            t_direction = slideDir;
            t_adjustedSpeed *= slideModifier;
            slideTime -= Time.deltaTime;
            if (slideTime <= 0)
            {
                sliding = false;
                weaponParentCurrPos -= Vector3.down * (slideAmount - crouchAmount);
            }
        }
        Vector3 t_targetVelocity = t_direction * t_adjustedSpeed * Time.deltaTime;
        t_targetVelocity.y = rb.velocity.y;
        rb.velocity = t_targetVelocity;

        //sliding
        if (isSliding)
        {
            sliding = true;
            slideDir = t_direction;
            slideTime = slideLenght;
            //adjust camera
            weaponParentCurrPos += Vector3.down * (slideAmount - crouchAmount);
            if (!crouched)
            {
                photonView.RPC("SetCrouch", RpcTarget.All, true);
            }
        }
        //jet
        if (jump && !grounded)
        {
            canJet = true;
        }
        if (grounded)
        {
            canJet = false;
            if (currentRecovery < jetWait)
            {
                currentRecovery = Mathf.Min(jetWait, currentRecovery + Time.fixedDeltaTime);
            }
            else
            {
                currentFuel = Mathf.Min(maxFuel, currentFuel + Time.fixedDeltaTime * jetRecovery);
            }
        }
        if (canJet && jet && currentFuel > 0)
        {
            rb.AddForce(Vector3.up * jetForce * Time.fixedDeltaTime, ForceMode.Acceleration);
            currentFuel = Mathf.Max(0, currentFuel - Time.fixedDeltaTime);
        }
        uiFuelbar.localScale = new Vector3(currentFuel / maxFuel, 1, 1);

        //Camera stuff
        if (sliding)
        {
            normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV * sprintFOVModifier * 1.25f, Time.deltaTime * 8f);
            normalCam.transform.localPosition = Vector3.MoveTowards(normalCam.transform.localPosition, origin + Vector3.down * slideAmount, Time.deltaTime);
        }
        else
        {
            normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV, Time.deltaTime * 8f);
            if (crouched)
            {
                normalCam.transform.localPosition = Vector3.Lerp(normalCam.transform.localPosition, origin + Vector3.down * crouchAmount, Time.deltaTime*6f);
            }
            else
            {
                normalCam.transform.localPosition = Vector3.Lerp(normalCam.transform.localPosition, origin, Time.deltaTime*6f);
            }

        }

        //animations
        float t_anim_horizontal=0f;
        float t_anim_vertical=0f;
        if(grounded){
            //t_anim_horizontal=t_direction.y;
            //t_anim_vertical=t_direction.x;
            t_anim_horizontal=t_hmove;
            t_anim_vertical=t_vmove;
        }
        animator.SetFloat("VelX",t_anim_horizontal);
        animator.SetFloat("VelY",t_anim_vertical);
    }

    void HeadBob(float p_z, float p_x_intensity, float p_y_intensity)
    {
        float t_aim_adjust = 1f;
        if (weaponUI.isAiming)
        {
            t_aim_adjust = 0.1f;
        }
        targetWeaponBobPosition = weaponParentCurrPos + new Vector3(Mathf.Cos(p_z) * p_x_intensity * t_aim_adjust, Mathf.Sin(p_z * 2) * p_y_intensity * t_aim_adjust, 0);
    }
    void SetY()
    {
        float t_input = Input.GetAxis("Mouse Y") * ySensitivity * Time.deltaTime;
        Quaternion t_adj = Quaternion.AngleAxis(t_input, -Vector3.right);
        Quaternion t_delta = cams.localRotation * t_adj;
        if (Quaternion.Angle(camCenter, t_delta) < maxAngle)
        {
            cams.localRotation = t_delta;
        }
        weapon.rotation = cams.rotation;

    }
    void SetX()
    {
        float t_input = Input.GetAxis("Mouse X") * xSensitivity * Time.deltaTime;
        Quaternion t_adj = Quaternion.AngleAxis(t_input, Vector3.up);
        Quaternion t_delta = player.localRotation * t_adj;
        player.localRotation = t_delta;


    }
    void UpdateCursorLock()
    {
        if (cursorLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                cursorLocked = false;
            }
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                cursorLocked = true;
            }
        }
    }
    public void TakeDamage(int p_damage)
    {
        if (photonView.IsMine)
        {
            currentHealth -= p_damage;
            Debug.Log(currentHealth);
            RefreshHealthBar();
            if (currentHealth <= 0)
            {
                Debug.Log("You died");
                PhotonNetwork.Destroy(gameObject);
                playerManager.CreateController();
            }
        }
    }

    void RefreshHealthBar()
    {
        float health_Ratio = (float)currentHealth / (float)maxHealth;
        uiHealthbar.localScale = Vector3.Lerp(uiHealthbar.localScale, new Vector3(health_Ratio, 1, 1), Time.deltaTime * 8f);
    }
    private float aimAngle;
    //receives updates frequently about stuff
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext((int)(weaponParent.transform.localEulerAngles.x * 100f));
        }
        else
        {
            aimAngle = (int)stream.ReceiveNext() / 100f;
        }
    }
    [PunRPC]
    void SetCrouch(bool p_state)
    {
        if (crouched == p_state) return;
        crouched = p_state;
        if (crouched)
        {
            standingCollider.SetActive(false);
            crouchingCollider.SetActive(true);
            weaponParentCurrPos += Vector3.down * crouchAmount;
        }
        else
        {
            crouchingCollider.SetActive(false);
            standingCollider.SetActive(true);
            weaponParentCurrPos -= Vector3.down * crouchAmount;
        }
    }
}
