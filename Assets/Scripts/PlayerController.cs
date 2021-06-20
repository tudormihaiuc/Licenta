using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine.UI;
using TMPro;

//class that manages everything that involves the player, from the movement, instantiation of the player prefab, 
//destroying it on death etc.
public class PlayerController : MonoBehaviourPunCallbacks, IPunObservable
{
    [SerializeField] float mouseSensitivity, sprintModifier, walkSpeed, jumpForce, smoothTime;
    //walkSpeed=speed, sprintSpeed=sprintModifier;
    //slideSpeed=slideModifier
    bool grounded;//boolean var keeping if the player is on the ground or not
    Vector3 smoothMoveVelocity;
    Vector3 moveAmount;
    Rigidbody rb;//the rigidbody of the player controller
    PhotonView photonView;

    //public vars
    public Transform weaponParent;
    public Transform player;//position of the player
    public Transform cams;
    public Transform weapon;
    public float xSensitivity;//mouse sensitivity on the X axis 
    public float ySensitivity;//mouse sensitivity on the Y axis 
    public float maxAngle;//sets the maxium degrees the player can look above
    public static bool cursorLocked = true;
    public int maxHealth;//initial health of the player when he spawns
    public float crouchModifier;//by how much does the crouch effect the movement speed
    public float crouchAmount;//how much the camera moves when crouching
    public float slideAmount;//how much the camera moves when sliding
    public GameObject standingCollider;
    public GameObject mesh;//the mesh of the player prefab
    public GameObject crouchingCollider;
    public float slideLenght;
    public float slideModifier;//by how much does the slide effect the movement speed
    public Camera normalCam;

    //jetpack
    public float jetForce;
    public float jetWait;//the amount of time between the last use of the jetpack and the start of refueling
    public float maxFuel;//the max amount of fuel the jetpack has
    public float jetRecovery;//the amount of time it takes the jetpack to refuel
    public ProfileData playerProfile;
    public TextMeshPro playerUsername;
    public AudioClip deathSound;
    public AudioClip footstep;
    public AudioClip jumpSound;
    public AudioClip landSound;
    public AudioClip healSound;
    public AudioClip jetpackSound;
    public AudioClip explosionSound;
    public AudioSource sfx;
    public ParticleSystem jetpack;//the particle system that appears whenever the player uses the jetpack
    public GameObject explosionFx;//the effect that appears when  the enemy ai hits the player

    //private vars
    private Vector3 weaponParentOrigin;
    private Vector3 weaponParentCurrPos;
    private float movementCounter;//variable that depending of the movement state changes accordingly and used for the headbob
    private float idleCounter;//var used for headbob when the player is not moving
    private Vector3 targetWeaponBobPosition;
    private Quaternion camCenter;
    private int currentHealth;
    private PlayerManager playerManager;//instance of the player manager script
    private Transform uiHealthbar;
    private Weapon weaponUI;
    private Text uiAmmo;
    private bool sliding;//var that checks if the player is currently sliding or not
    private float slideTime;//the ammount of time the slide takes to complete
    private Vector3 slideDir;//the slide direction
    private float baseFOV;//var used for changing the field of view of the player when he is sliding
    private float sprintFOVModifier = 1.2f;
    private Vector3 origin;
    private bool crouched;//var that checks if the player is currently crouching or not
    private bool isAiming;//var that checks if the player is currently aiming or not
    private float currentFuel;
    private float currentRecovery; //the time passed at the moment from the m oment when the last jet ended
    private Transform uiFuelbar;
    private bool canJet;//boolean var that checks if a player can use the jetpack
    private Animator animator;//the animator component for the player
    private Weapon weaponAmmo;
    GameObject x;//var used for the ammo and health pickups, used to deactivate the pickup after it has been used by a player
    private Text uiUsername;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        photonView = GetComponent<PhotonView>();
    }

    void Start()
    {
        Application.targetFrameRate = 50;
        QualitySettings.vSyncCount = 0;//makes sure that vsync is disabled
        camCenter = cams.localRotation;
        currentHealth = maxHealth;
        baseFOV = normalCam.fieldOfView;//set the base field of view
        origin = normalCam.transform.localPosition;//sets the origin position of the camera
        weaponUI = GetComponent<Weapon>();
        playerManager = GameObject.Find("PlayerManager").GetComponent<PlayerManager>();
        weaponParentOrigin = weaponParent.localPosition;
        weaponParentCurrPos = weaponParentOrigin;
        currentFuel = maxFuel;
        photonView.RPC("StartJetpack", RpcTarget.All);//start the particle system for the jetpack
        photonView.RPC("DisableJetpack", RpcTarget.All);//disable the system for the moment, activates only when a player is jetting
        if (photonView.IsMine)
        {
            //find all the ui elements
            uiHealthbar = GameObject.Find("HUD/Health/Bar").transform;
            uiAmmo = GameObject.Find("HUD/Ammo/Text").GetComponent<Text>();
            uiUsername = GameObject.Find("HUD/Username/Text").GetComponent<Text>();
            RefreshHealthBar();
            uiUsername.text = Launcher.myProfile.username;
            uiFuelbar = GameObject.Find("HUD/Fuel/Bar").transform;

            animator = GetComponent<Animator>();
        }
        else
        {
            //if this is not my instance of the game, it means that all the other player prefabs the camera sees are other players
            //so change the layer to 11 (11 is the Player layer, initialy the player prefab collider and mesh are on the Local Player layer
            //so the player's camera cant see its own body)
            Destroy(GetComponentInChildren<Camera>().gameObject);
            Destroy(rb);
            gameObject.layer = 11;
            standingCollider.layer = 11;
            crouchingCollider.layer = 11;
            ChangeLayer(mesh.transform, 11);
        }
    }

    //starts the particle system
    [PunRPC]
    private void StartJetpack()
    {
        jetpack.Play();
    }

    //enables the particle system
    [PunRPC]
    private void EnableJetpack()
    {
        jetpack.enableEmission = true;
    }

    //disables the particle system
    [PunRPC]
    private void DisableJetpack()
    {
        jetpack.enableEmission = false;
    }

    //changes the layer of the player recursively
    private void ChangeLayer(Transform p_transform, int p_layer)
    {
        p_transform.gameObject.layer = p_layer;
        foreach (Transform t in p_transform)
        {
            ChangeLayer(t, p_layer);
        }
    }

    //syncs the username text to the player's profile name
    [PunRPC]
    private void SyncProfile(string p_username, int p_level, int p_xp)
    {
        playerProfile = new ProfileData(p_username, p_level, p_xp);
        playerUsername.text = playerProfile.username;
    }
    [PunRPC]
    public void PlayFootstep()
    {
        sfx.PlayOneShot(footstep);
    }
    [PunRPC]
    public void PlayJumpSound()
    {
        sfx.PlayOneShot(jumpSound);
    }
    [PunRPC]
    public void PlayJetpackSound()
    {
        sfx.PlayOneShot(jetpackSound);
    }
    public void PlayLandSound()
    {
        if (grounded)
            sfx.PlayOneShot(landSound);
    }
    void Update()
    {
        //if i dont use this, each player would control eachother player
        if (!photonView.IsMine)
        {
            RefreshMultiplayerState();
            return;
        }
        UpdateCursorLock();
        if (photonView.IsMine)
        {
            if (grounded == true && rb.velocity.magnitude > 2f)
            {
                //photonView.RPC("PlayFootstep", RpcTarget.All);
            }
            photonView.RPC("SyncProfile", RpcTarget.All, Launcher.myProfile.username, Launcher.myProfile.level, Launcher.myProfile.xp);
        }
        float t_hmove = Input.GetAxisRaw("Horizontal");//the horizontal movement
        float t_vmove = Input.GetAxisRaw("Vertical");//the vertical movement
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
            //if the game is paused, dont let the player move
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
        if (isCrouching)
        {
            //call the SetCrouch function over the net, so all the players see the other player crouches
            photonView.RPC("SetCrouch", RpcTarget.All, !crouched);
        }
        if (isJumping)
        {
            if (crouched)
            {
                //if the player is crouched and he jumps, disable the crouch
                photonView.RPC("SetCrouch", RpcTarget.All, false);
            }
            //jump
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
            TakeDamage(100, -1);
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
    void Move()
    {
        Vector3 moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
        moveAmount = Vector3.SmoothDamp(moveAmount, moveDir * (Input.GetKey(KeyCode.LeftShift) ? sprintModifier : walkSpeed), ref smoothMoveVelocity, smoothTime);

    }

    //plays the jumping animation
    [PunRPC]
    void Jump()
    {
        player.GetComponent<Animator>().Play("Jump", 0, 0);
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        //called when the info is received
        if (!photonView.IsMine && targetPlayer == photonView.Owner)
        {
            //sync the curent item
            //EquipItem((int)changedProps["itemIndex"]);
        }
    }

        //we are doing the calculations here because the method Update() is called every farme 
        //and we dont want our calculations be impacted by the frame rate
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
                    //if the player is sprinting, disable the crouch 
                    photonView.RPC("SetCrouch", RpcTarget.All, false);
                }
                t_adjustedSpeed *= sprintModifier;//adjust the player speed by the sprint modifier
            }
            else if (crouched)
            {
                t_adjustedSpeed *= crouchModifier;//adjust the player speed by the crouch modifier
            }
        }
        else
        {
            //sliding
            t_direction = slideDir;
            t_adjustedSpeed *= slideModifier;//adjust the player speed by the slide modifier
            slideTime -= Time.deltaTime;
            if (slideTime <= 0)
            {   //if the slide timer ended, stop the slide
                sliding = false;
                weaponParentCurrPos -= Vector3.down * (slideAmount - crouchAmount);//move the weapon back to its default position
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
            //let the player jet only if he already jumped
            canJet = true;
        }
        if (grounded)
        {
            //don't let the player jet while he is on the ground
            canJet = false;
            if (currentRecovery < jetWait)
            {
                //if the time that needs to pass so the jetpack can refuel did not pass, update the currently passed time
                currentRecovery = Mathf.Min(jetWait, currentRecovery + Time.fixedDeltaTime);
            }
            else
            {
                //if the time to refuel passed, refuel the jetpack
                currentFuel = Mathf.Min(maxFuel, currentFuel + Time.fixedDeltaTime * jetRecovery);
            }
        }
        if (canJet && jet && currentFuel > 0)
        {
            //if there is fuel, and the player can jet, and he presses the correct key, add upwards force
            rb.AddForce(Vector3.up * jetForce * Time.fixedDeltaTime, ForceMode.Acceleration);
            currentFuel = Mathf.Max(0, currentFuel - Time.fixedDeltaTime);//deplete the fuel
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //if the space key is pressed, enable the jetpack particle system
            photonView.RPC("EnableJetpack", RpcTarget.All);
            photonView.RPC("PlayJetpackSound", RpcTarget.All);
        }
        if (Input.GetKeyUp(KeyCode.Space))
        {
            //if the player stops pressing the key, stop the effect
            photonView.RPC("DisableJetpack", RpcTarget.All);
        }
        //update the fuel ui
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
                normalCam.transform.localPosition = Vector3.Lerp(normalCam.transform.localPosition, origin + Vector3.down * crouchAmount, Time.deltaTime * 6f);
            }
            else
            {
                normalCam.transform.localPosition = Vector3.Lerp(normalCam.transform.localPosition, origin, Time.deltaTime * 6f);
            }

        }

        //animations
        //set the corespondent variables in the animator, so it knows what animation to play accordingly to the current state
        animator.SetBool("jump", grounded);
        animator.SetBool("sprint", isSprinting);
        animator.SetBool("slide", sliding);
        float t_anim_horizontal = 0f;
        float t_anim_vertical = 0f;
        if (grounded)
        {
            t_anim_horizontal = t_hmove;
            t_anim_vertical = t_vmove;
        }
        animator.SetFloat("VelX", t_anim_horizontal);
        animator.SetFloat("VelY", t_anim_vertical);
    }


    //sets the camera movement accordingly to every movemet state, so it appears as the player head is really moving
    void HeadBob(float p_z, float p_x_intensity, float p_y_intensity)
    {
        float t_aim_adjust = 1f;
        if (weaponUI.isAiming)
        {
            t_aim_adjust = 0.1f;
        }
        targetWeaponBobPosition = weaponParentCurrPos + new Vector3(Mathf.Cos(p_z) * p_x_intensity * t_aim_adjust, Mathf.Sin(p_z * 2) * p_y_intensity * t_aim_adjust, 0);
    }

    //gets the Y axis mouse input and modifies it with the given sensitivity
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

    //gets the X axis mouse input and modifies it with the given sensitivity
    void SetX()
    {
        float t_input = Input.GetAxis("Mouse X") * xSensitivity * Time.deltaTime;
        Quaternion t_adj = Quaternion.AngleAxis(t_input, Vector3.up);
        Quaternion t_delta = player.localRotation * t_adj;
        player.localRotation = t_delta;


    }
    
    //enables/disables the mouse cursor if the Escape key is pressed
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

    //makes the player able to take damage and die and takes as parameters the damage received and the player who sends the damage
    public void TakeDamage(int p_damage, int p_actor)
    {
        if (photonView.IsMine)
        {
            currentHealth -= p_damage;
            //sfx.PlayOneShot(deathSound);
            Debug.Log(currentHealth);
            RefreshHealthBar();
            if (currentHealth <= 0)
            {
                sfx.PlayOneShot(deathSound);
                Debug.Log("You died");
                PhotonNetwork.Destroy(gameObject);//destroy the player
                //when the player dies update his deaths 
                playerManager.ChangeStat_S(PhotonNetwork.LocalPlayer.ActorNumber, 1, 1);
                if (p_actor >= 0)
                {   
                    //update the killer's kills
                    playerManager.ChangeStat_S(p_actor, 0, 1);
                }
                playerManager.CreateController();//spawn the player 
            }
        }
    }

    //updates the player health ui
    void RefreshHealthBar()
    {
        float health_Ratio = (float)currentHealth / (float)maxHealth;
        uiHealthbar.localScale = Vector3.Lerp(uiHealthbar.localScale, new Vector3(health_Ratio, 1, 1), Time.deltaTime * 8f);
    }
    private float aimAngle;

    //receives updates frequently about every player's weapon movement so photon can syncronize it on every computer
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

    //enables/disables the crouch state
    [PunRPC]
    void SetCrouch(bool p_state)
    {
        if (crouched == p_state) return;
        crouched = p_state;
        if (crouched)
        {
            //if the player wants to crouch
            standingCollider.SetActive(false);//disable the standing collider
            crouchingCollider.SetActive(true);//enable the crouch collider
            weaponParentCurrPos += Vector3.down * crouchAmount;//move the weapon down
            animator.SetBool("crouch", true);//start the crouch animation
        }
        else
        {
            //if the player is crouching and he stands up
            crouchingCollider.SetActive(false);//disable the crouch collider
            standingCollider.SetActive(true);//enables the standing collider
            weaponParentCurrPos -= Vector3.down * crouchAmount;//moves the weapon back up
            animator.SetBool("crouch", false);//disables the crouch animation
        }
    }

    //if the player collides with other specific colliders, do stuff
    private void OnTriggerEnter(Collider other)
    {
        //if the player enters the ammo pickup collider
        if (other.gameObject.tag.Equals("Ammo"))
        {
            Debug.Log("entered collision");
            weaponAmmo = GetComponent<Weapon>();
            weaponAmmo.GetAmmo();//refresh its ammo
            sfx.PlayOneShot(landSound);//play a sound so the player gets a auditive feedback that his ammo changed
            x = other.transform.parent.gameObject;
            DisableAndEnable();//disable the pickup for a given time so the player can't use it again immediately
        }
        //if the player enteres the health pickup collider
        if (other.gameObject.tag.Equals("Heal"))
        {
            //if he is not at max health
            if (currentHealth < maxHealth)
            {
                //heal him
                currentHealth = maxHealth;
                sfx.PlayOneShot(healSound);//play a sound so the player gets a auditive feedback that his health changed
                x = other.transform.parent.gameObject;
                DisableAndEnable();//disable the pickup for a given time so the player can't use it again immediately
            }
        }
        //if the player enters the collider of an enemy projectile
        if (other.gameObject.tag.Equals("EnemyProjectile"))
        {
            Debug.Log("grenade");
            currentHealth -= 100;//damage him
            //instantiate an explosion effect
            GameObject t_newExplosionFx = Instantiate(explosionFx, other.gameObject.transform.position + transform.forward, Quaternion.identity) as GameObject;
            sfx.PlayOneShot(explosionSound);//play a sound 
            //if his health drops to zero or below 
            if (currentHealth <= 0)
            {
                sfx.PlayOneShot(deathSound);
                //kill him
                Debug.Log("You died");
                PhotonNetwork.Destroy(gameObject);
                playerManager.CreateController();//spawn him back
            }
        }
        //Destroy(other.gameObject);
    }

    //enumerator for the pickup refresh timer
    [PunRPC]
    IEnumerator Wait()
    {
        Debug.Log("before wait");
        x.SetActive(false);//disable the pickup
        yield return new WaitForSeconds(30);//after 30 seconds
        x.SetActive(true);//enable it again
        Debug.Log("after wait");
    }

    //starts the coroutine for the pickups
    [PunRPC]
    public void DisableAndEnable()
    {
        StartCoroutine(Wait());
    }
    
}
