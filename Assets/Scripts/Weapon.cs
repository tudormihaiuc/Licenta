using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;

//hendles the functionality of the guns (shooting, reloading, equiping etc.)
public class Weapon : MonoBehaviourPunCallbacks
{
    public List<Gun> loadout;//the current loadout of guns of the player
    public Transform weaponParent;
    public GameObject bulletHoldeFx;//the effect instantiated on a surface when a bullet hits
    public GameObject bulletholePrefab;//the sprite instantiated when a bullet hits
    public LayerMask canBeShot;//layer mask used to set what surfaces can a bullet hit
    public float currentCooldown;//the ammount of time between the player can shoot the gun again
    public Camera camera;
    public bool isAiming = false;//boolean var to check if the player is currently aiming the gun
    [HideInInspector] public Gun currentGunData;//the info of a gun
    public AudioSource sfx;
    public AudioClip hitmarkerSound;
    private GameObject currentWeapon;
    private int currentIndex;//the current position in the loadout array
    private GameObject currWeaponPosition;
    private bool isReloading;//boolean var to check if the player is currently reloading the gun
    private Image hitmarkerImage;//image instantiated when the player hits another player
    private float hitmarkerWait;//the amount of time the hiotmarker image is visible on the screen
    private float bulletHoleWait;
    Coroutine lastRoutine = null;


    // Start is called before the first frame update
    void Start()
    {
        // at the runtime of the script initialize the ammo of all the guns currently in the player's loadout
        foreach (Gun i in loadout)
        {
            if (photonView.IsMine)
                i.InitAmmo();
        }
        Equip(1);//equip the weapon at the index 1 from the loadout array
        hitmarkerImage = GameObject.Find("HUD/Hitmarker/Image").GetComponent<Image>();//find the hitmarker image from the HUD prefab
        Debug.Log("hitmarker image found");
        hitmarkerImage.color = new Color(1, 1, 1, 0);//set the color of the hitmarker image
    }

    // Update is called once per frame
    void Update()
    {
        //if it is the player instance of the game
        if (photonView.IsMine && Input.GetKeyDown(KeyCode.Alpha1))
        {
            //for the 1 key, equip the first weapon 
            photonView.RPC("Equip", RpcTarget.All, 0);
        }
        if (photonView.IsMine && Input.GetKeyDown(KeyCode.Alpha2))
        {
            //for the 2 key, equip the second weapon 
            photonView.RPC("Equip", RpcTarget.All, 1);
        }
        if (photonView.IsMine && Input.GetKeyDown(KeyCode.Alpha3))
        {
            photonView.RPC("Equip", RpcTarget.All, 2);
        }
        if (currentWeapon != null)
        {
            if (photonView.IsMine)
            {
                //if the second mouse button is pressed, call the Aim function over the network
                photonView.RPC("Aim", RpcTarget.All, Input.GetMouseButton(1));
                //if the current weapon is not automatic
                if (loadout[currentIndex].burst != 1)
                {
                    //if the first mouse button is pressed and the weapon can shoot
                    if (Input.GetMouseButtonDown(0) && currentCooldown <= 0)
                    {
                        if (loadout[currentIndex].FireBullet())
                        {
                            //for each pellet, or 1 in case the weapon is not a shotgun
                            for (int i = 0; i < Mathf.Max(1, currentGunData.pellets); i++)
                            {
                                Vector3 firingSpot = camera.transform.position;//set the firing spot where the player is looking
                                Vector3 fireDirection = camera.transform.forward;//set the direction
                                Vector3 t_bloom = fireDirection * 1000f + firingSpot;
                                //make the bullet hit location random, based on the bloom var of each gun
                                t_bloom += Random.Range(-loadout[currentIndex].bloom, loadout[currentIndex].bloom) * camera.transform.up;
                                t_bloom += Random.Range(-loadout[currentIndex].bloom, loadout[currentIndex].bloom) * camera.transform.right;
                                t_bloom -= firingSpot;
                                t_bloom.Normalize();
                                //if the gun is not reloading right now
                                if (isReloading == false)
                                    //call the Shoot function over the network
                                    photonView.RPC("Shoot", RpcTarget.All, firingSpot, t_bloom);
                                //photonView.RPC("SyncGunPosition",RpcTarget.All);
                            }
                        }
                        else
                        {
                            ReloadRPC();
                        }
                    }
                }
                else
                {
                    if (Input.GetMouseButton(0) && currentCooldown <= 0)
                    {
                        if (loadout[currentIndex].FireBullet())
                        {
                            for (int i = 0; i < Mathf.Max(1, currentGunData.pellets); i++)
                            {
                                Vector3 firingSpot = camera.transform.position;
                                Vector3 fireDirection = camera.transform.forward;
                                Vector3 t_bloom = fireDirection * 1000f + firingSpot;
                                t_bloom += Random.Range(-loadout[currentIndex].bloom, loadout[currentIndex].bloom) * camera.transform.up;
                                t_bloom += Random.Range(-loadout[currentIndex].bloom, loadout[currentIndex].bloom) * camera.transform.right;
                                t_bloom -= firingSpot;
                                t_bloom.Normalize();
                                photonView.RPC("Shoot", RpcTarget.All, firingSpot, t_bloom);
                                //photonView.RPC("SyncGunPosition",RpcTarget.All);
                            }
                        }
                        else
                        {
                            ReloadRPC();
                        }
                    }
                }
                //if the reload key R is pressed
                if (Input.GetKeyDown(KeyCode.R))
                {
                    if (loadout[currentIndex].GetClip() != loadout[currentIndex].GetClipSize())
                        //call the reload function over the net
                        photonView.RPC("ReloadRPC", RpcTarget.All);
                }
                //cooldown
                if (currentCooldown > 0)
                {
                    currentCooldown -= Time.deltaTime;
                }
            }
            currentWeapon.transform.localPosition = Vector3.Lerp(currentWeapon.transform.localPosition, Vector3.zero, Time.deltaTime * 4f);
        }
        if (photonView.IsMine)
        {
            if (hitmarkerWait > 0)
            {
                hitmarkerWait -= Time.deltaTime;
            }
            else
            {
                //start fading the hitmarker
                hitmarkerImage.color = Color.Lerp(hitmarkerImage.color, new Color(1, 1, 1, 0), Time.deltaTime * 2f);
            }
            if (bulletHoleWait > 0)
            {
                bulletHoleWait -= Time.deltaTime;
            }
            else
            {
                SpriteRenderer bulletHoleRenderer = bulletholePrefab.GetComponent<SpriteRenderer>();
                bulletHoleRenderer.sharedMaterial.color = Color.Lerp(bulletHoleRenderer.sharedMaterial.color, new Color(1, 1, 1, 0), Time.deltaTime*2f);
            }
        }

        if (Pause.paused && photonView.IsMine)
        {
            return;
        }

    }

    //function used for aming a weapon
    [PunRPC]
    void Aim(bool p_isAiming)
    {
        //makes aimg when reloading impossible
        if (isReloading)
        {
            return;
        }
        isAiming = p_isAiming;
        Transform t_anchor = currentWeapon.transform.Find("Root");
        Transform t_state_hip = currentWeapon.transform.Find("States/Hip");
        Transform t_state_ads = currentWeapon.transform.Find("States/ADS");
        if (p_isAiming)
        {
            //aim
            //lerp between the current position of the gun to the ads state posiition with the given speed of the gun 
            t_anchor.position = Vector3.Lerp(t_anchor.position, t_state_ads.position, Time.deltaTime * loadout[currentIndex].aimSpeed);
        }
        else
        {
            //hip
            //lerp between the current position of the gun to the hipfire state posiition with the given speed of the gun
            t_anchor.position = Vector3.Lerp(t_anchor.position, t_state_hip.position, Time.deltaTime * loadout[currentIndex].aimSpeed);
        }
    }

    //function used to equip a weapon
    [PunRPC]
    void Equip(int p_ind)
    {
        if (currentWeapon != null)
        {
            //if the gun is currently reloading 
            if (isReloading)
            {
                //stop the reloading
                StopCoroutine(lastRoutine);
                isReloading = false;
            }
            Destroy(currentWeapon);//destroy the current weapon
        }
        currentIndex = p_ind;
        //instantiate the new weapoon
        GameObject t_newWeapon = Instantiate(loadout[p_ind].prefab, weaponParent.position, weaponParent.rotation, weaponParent) as GameObject;
        t_newWeapon.transform.localPosition = Vector3.zero;
        t_newWeapon.transform.localEulerAngles = Vector3.zero;
        t_newWeapon.GetComponent<Sway>().isMine = photonView.IsMine;//get the sway of the new weapon
        t_newWeapon.GetComponent<Animator>().Play("Equip", 0, 0);//play the Equip animation for the new weapon
        currentWeapon = t_newWeapon;//make the current weapon the new weapon
        currentGunData = loadout[p_ind];//modify the loadout
    }

    //function used for shooting a gun
    [PunRPC]
    void Shoot(Vector3 firingPoint, Vector3 firingDirection)
    {
        if (currentWeapon != null)
        {
            if (isReloading)
            {   //if the gun is currently reloading, stop the reload
                Debug.Log("stop reloading");
                StopCoroutine(lastRoutine);
                isReloading = false;
            }
        }
        //rate of fire
        currentCooldown = loadout[currentIndex].firerate;
        //raycast
        RaycastHit t_hit = new RaycastHit();
        Ray shooterRay = new Ray(firingPoint, firingDirection);
        //if the raycast started from the gun with the forward direction hits a surface that can be shot 
        if (Physics.Raycast(shooterRay, out t_hit, 1000f, canBeShot))
        {
            //at the place of impact instantiate the bullet hole
            GameObject t_newHole = Instantiate(bulletholePrefab, t_hit.point + t_hit.normal * 0.001f, Quaternion.identity) as GameObject;
            //at the place of impact instantiate the bullet hole hit  effect
            GameObject t_newBulletFx = Instantiate(bulletHoldeFx, t_hit.point + t_hit.normal * 0.001f, Quaternion.identity) as GameObject;
            t_newHole.transform.LookAt(t_hit.point + t_hit.normal);//make sure that the bullet hole is facing the player that shot it
            t_newBulletFx.transform.LookAt(t_hit.point + t_hit.normal);//make sure that the bullet hole effect is facing the player that shot it
            bulletHoleWait=1.5f;
            Destroy(t_newHole, 1.5f);//after 1.5 seconds destroy the bullehole
            //if it is the player's instance of the game
            if (photonView.IsMine)
            {
                //for each weapon name, play the corresponding Shoot animation
                if (loadout[currentIndex].name == "Pistol")
                {
                    currentWeapon.GetComponent<Animator>().Play("Shoot", 0, 0);
                }
                else if (loadout[currentIndex].name == "Machine Gun")
                {
                    currentWeapon.GetComponent<Animator>().Play("Shoot", 0, 0);
                }
                else if (loadout[currentIndex].name == "M4")
                {
                    currentWeapon.GetComponent<Animator>().Play("Shoot", 0, 0);
                }
                //if shooting a player
                if (t_hit.collider.gameObject.layer == 11)
                {
                    //RPC call to dmg player
                    t_hit.collider.transform.root.gameObject.GetPhotonView().RPC("TakeDamage", RpcTarget.All, loadout[currentIndex].damage, PhotonNetwork.LocalPlayer.ActorNumber);
                    //hitmarker
                    hitmarkerImage.color = Color.white;
                    sfx.PlayOneShot(hitmarkerSound);
                    hitmarkerWait = 0.5f;
                }
                //if shooting Ai 
                if (t_hit.collider.gameObject.layer == 13)
                {
                    //call AiTakeDamage function
                    t_hit.collider.transform.root.gameObject.GetComponent<EnemyAi>().AiTakeDamage(loadout[currentIndex].damage);
                    hitmarkerImage.color = Color.white;
                    sfx.PlayOneShot(hitmarkerSound);
                    hitmarkerWait = 0.5f;
                }
            }
        }

        //sound effects
        sfx.Stop();
        sfx.clip = currentGunData.gunshotSound;
        sfx.pitch = 1 - currentGunData.soundRandomization + Random.Range(-currentGunData.soundRandomization, currentGunData.soundRandomization);
        sfx.volume = currentGunData.gunVolume;
        sfx.Play();
        //gun effects
        currentWeapon.transform.Rotate(-loadout[currentIndex].recoil, 0, 0);
        currentWeapon.transform.position -= currentWeapon.transform.forward * loadout[currentIndex].kickback;
        if (currentGunData.recovery)
        {
            currentWeapon.GetComponent<Animator>().Play("Recovery", 0, 0);
        }
    }

    //Rpc for player taking damage, so the player's current health is updated on every other player's computer
    [PunRPC]
    private void TakeDamage(int p_damage, int p_actor)
    {
        //calls the TakeDamage function
        GetComponent<PlayerController>().TakeDamage(p_damage, p_actor);
    }

    [PunRPC]
    public void SyncGunPosition()
    {
        currentWeapon.transform.localPosition = Vector3.Lerp(currentWeapon.transform.localPosition, Vector3.zero, Time.deltaTime * 20f);
    }

    //function used for reloading 
    IEnumerator Reload(float p_wait)
    {
        //if there is any ammo left
        if (loadout[currentIndex].GetStash() > 0)
        {
            isReloading = true;
            //for each weapon name, play the corresponding Reload animation
            if (loadout[currentIndex].name == "Pistol")
            {
                currentWeapon.GetComponent<Animator>().Play("Reload1", 0, 0);
            }
            else if (loadout[currentIndex].name == "Machine Gun")
            {
                currentWeapon.GetComponent<Animator>().Play("Reload1", 0, 0);
            }
            else if (loadout[currentIndex].name == "Shotgun")
            {
                currentWeapon.GetComponent<Animator>().Play("Reload", 0, 0);
            }
            else if (loadout[currentIndex].name == "M4")
            {
                currentWeapon.GetComponent<Animator>().Play("Reload", 0, 0);
            }

            yield return new WaitForSeconds(p_wait);//automatically wait for p_wait ammount of time without freezing the script
            loadout[currentIndex].ReloadGun();//call the ReloadGun function from the Gun class
            currentWeapon.SetActive(true);
            isReloading = false;
        }
    }

    //Rpc for rloading so the reload animation is played for every other player
    [PunRPC]
    private void ReloadRPC()
    {
        lastRoutine = StartCoroutine(Reload(loadout[currentIndex].reload));
    }

    //functiomn used to refresh the Ui for ammo
    public void RefreshAmmo(Text p_text)
    {
        int t_clip = loadout[currentIndex].GetClip();
        int t_stash = loadout[currentIndex].GetStash();
        p_text.text = t_clip.ToString("D2") + "/" + t_stash.ToString("D2");
    }
    public void GetAmmo()
    {
        foreach (Gun i in loadout)
        {
            if (photonView.IsMine)
                i.InitAmmoWithoutReloading();
        }
    }

    //function used for the weapon pickup
    [PunRPC]
    void PickupWeapon(string name)
    {
        //find weapon from a library
        Gun newWeapon = GunLibrary.FindGun(name);
        //Initialize its ammo
        newWeapon.InitAmmo();
        //if the player already has 2 or more weapons equiped
        if (loadout.Count >= 2)
        {
            //change the second Weapon, the first being the pistol every time
            if (newWeapon.name != loadout[1].name)
            {
                loadout[1] = newWeapon;
                Equip(currentIndex);
            }
        }
        //else, add the picked up weapon to the loadout
        else
        {
            loadout.Add(newWeapon);
            Equip(loadout.Count - 1);
        }
    }
}
