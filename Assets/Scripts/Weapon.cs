using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;

public class Weapon : MonoBehaviourPunCallbacks
{
    public List<Gun> loadout;
    public Transform weaponParent;

    private GameObject currentWeapon;
    private int currentIndex;

    public GameObject bulletholePrefab;
    public LayerMask canBeShot;
    public float currentCooldown;

    public Camera camera;
    private GameObject currWeaponPosition;
    private bool isReloading;
    public bool isAiming = false;
    [HideInInspector] public Gun currentGunData;
    public AudioSource sfx;
    private Image hitmarkerImage;
    private float hitmarkerWait;
    public AudioClip hitmarkerSound;
    Coroutine lastRoutine = null;
    public GameObject bulletHoldeFx;

    // Start is called before the first frame update
    void Start()
    {
        foreach (Gun i in loadout)
        {
            if (photonView.IsMine)
                i.InitAmmo();
        }
        Equip(1);
        hitmarkerImage = GameObject.Find("HUD/Hitmarker/Image").GetComponent<Image>();
        Debug.Log("hitmarker image found");
        hitmarkerImage.color = new Color(1, 1, 1, 0);
    }

    // Update is called once per frame
    void Update()
    {
        if (photonView.IsMine && Input.GetKeyDown(KeyCode.Alpha1))
        {
            photonView.RPC("Equip", RpcTarget.All, 0);
            //Equip(0);
        }
        if (photonView.IsMine && Input.GetKeyDown(KeyCode.Alpha2))
        {
            photonView.RPC("Equip", RpcTarget.All, 1);
            //Equip(0);
        }
        if (photonView.IsMine && Input.GetKeyDown(KeyCode.Alpha3))
        {
            photonView.RPC("Equip", RpcTarget.All, 2);
            //Equip(0);
        }
        if (currentWeapon != null)
        {
            if (photonView.IsMine)
            {
                photonView.RPC("Aim", RpcTarget.All, Input.GetMouseButton(1));

                //Aim(Input.GetMouseButton(1));
                if (loadout[currentIndex].burst != 1)
                {
                    if (Input.GetMouseButtonDown(0) && currentCooldown <= 0)
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
                                if (isReloading == false)
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
                            //loadout[currentIndex].Reload();
                            ReloadRPC();
                        }
                    }
                }
                if (Input.GetKeyDown(KeyCode.R))
                {
                    //loadout[currentIndex].Reload();
                    
                    if (loadout[currentIndex].GetClip() != loadout[currentIndex].GetClipSize())
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
                hitmarkerImage.color = Color.Lerp(hitmarkerImage.color, new Color(1, 1, 1, 0), Time.deltaTime * 2f);
                //Debug.Log("start changing color");
            }
        }

        if (Pause.paused && photonView.IsMine)
        {
            return;
        }

    }

    [PunRPC]
    void Aim(bool p_isAiming)
    {
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
            t_anchor.position = Vector3.Lerp(t_anchor.position, t_state_ads.position, Time.deltaTime * loadout[currentIndex].aimSpeed);
        }
        else
        {
            //hip
            t_anchor.position = Vector3.Lerp(t_anchor.position, t_state_hip.position, Time.deltaTime * loadout[currentIndex].aimSpeed);
        }
    }
    [PunRPC]
    void Equip(int p_ind)
    {
        if (currentWeapon != null)
        {
            if (isReloading)
            {
                StopCoroutine(lastRoutine);
                isReloading=false;
            }
            Destroy(currentWeapon);
        }
        currentIndex = p_ind;
        GameObject t_newWeapon = Instantiate(loadout[p_ind].prefab, weaponParent.position, weaponParent.rotation, weaponParent) as GameObject;
        t_newWeapon.transform.localPosition = Vector3.zero;
        t_newWeapon.transform.localEulerAngles = Vector3.zero;
        //if(photonView.IsMine)
        t_newWeapon.GetComponent<Sway>().isMine = photonView.IsMine;
        t_newWeapon.GetComponent<Animator>().Play("Equip", 0, 0);
        currentWeapon = t_newWeapon;
        currentGunData = loadout[p_ind];
        // currWeaponPosition.transform.localPosition=currentWeapon.transform.localPosition;
    }
    [PunRPC]
    void Shoot(Vector3 firingPoint, Vector3 firingDirection)
    {
        if (currentWeapon != null)
        {
            if (isReloading)
            {
                Debug.Log("stop reloading");
                StopCoroutine(lastRoutine);
                isReloading=false;
            }
        }
        //Transform t_spawn=transform.Find("Cameras/NormalCamera");
        //bloom
        /*Vector3 t_bloom=t_spawn.position+t_spawn.forward*1000f;
        t_bloom+=Random.Range(-loadout[currentIndex].bloom,loadout[currentIndex].bloom)*t_spawn.up;
        t_bloom+=Random.Range(-loadout[currentIndex].bloom,loadout[currentIndex].bloom)*t_spawn.right;
        t_bloom-=t_spawn.position;
        t_bloom.Normalize();*/
        //raycast
        //rate of fire
        currentCooldown = loadout[currentIndex].firerate;
        //raycast
        RaycastHit t_hit = new RaycastHit();
        Ray shooterRay = new Ray(firingPoint, firingDirection);
        if (Physics.Raycast(shooterRay, out t_hit, 1000f, canBeShot))
        {
            GameObject t_newHole = Instantiate(bulletholePrefab, t_hit.point + t_hit.normal * 0.001f, Quaternion.identity) as GameObject;
            GameObject t_newBulletFx= Instantiate(bulletHoldeFx, t_hit.point + t_hit.normal * 0.001f, Quaternion.identity) as GameObject;
            t_newHole.transform.LookAt(t_hit.point + t_hit.normal);
            t_newBulletFx.transform.LookAt(t_hit.point + t_hit.normal);
            Destroy(t_newHole, 1.5f);
            if (photonView.IsMine)
            {
                if (loadout[currentIndex].name == "Pistol")
                {
                    currentWeapon.GetComponent<Animator>().Play("Shoot", 0, 0);
                }
                else if (loadout[currentIndex].name == "Machine Gun")
                {
                    currentWeapon.GetComponent<Animator>().Play("Shoot", 0, 0);
                }else if (loadout[currentIndex].name == "M4")
                {
                    currentWeapon.GetComponent<Animator>().Play("Shoot", 0, 0);
                }
                //shooting a player
                if (t_hit.collider.gameObject.layer == 11)
                {
                    //RPC call to dmg player
                    t_hit.collider.transform.root.gameObject.GetPhotonView().RPC("TakeDamage", RpcTarget.All, loadout[currentIndex].damage, PhotonNetwork.LocalPlayer.ActorNumber);
                    //hitmarker
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
        //Debug.Log("gunshot" + currentGunData.gunshotSound.name);
        //gun effects
        currentWeapon.transform.Rotate(-loadout[currentIndex].recoil, 0, 0);
        currentWeapon.transform.position -= currentWeapon.transform.forward * loadout[currentIndex].kickback;
        if (currentGunData.recovery)
        {
            currentWeapon.GetComponent<Animator>().Play("Recovery", 0, 0);
        }



    }
    [PunRPC]
    private void TakeDamage(int p_damage, int p_actor)
    {
        GetComponent<PlayerController>().TakeDamage(p_damage, p_actor);
    }

    [PunRPC]
    public void SyncGunPosition()
    {
        currentWeapon.transform.localPosition = Vector3.Lerp(currentWeapon.transform.localPosition, Vector3.zero, Time.deltaTime * 20f);
    }

    IEnumerator Reload(float p_wait)
    {
        if(loadout[currentIndex].GetStash()>0){
        isReloading = true;
        //the lines with SetActive is a placeholder for the reload anim
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
        }else if (loadout[currentIndex].name == "M4")
        {
            currentWeapon.GetComponent<Animator>().Play("Reload", 0, 0);
        }

        yield return new WaitForSeconds(p_wait);//wait for p_wait ammount of time without freezing the script
        loadout[currentIndex].ReloadGun();
        currentWeapon.SetActive(true);
        isReloading = false;}
    }
    [PunRPC]
    private void ReloadRPC()
    {
        lastRoutine= StartCoroutine(Reload(loadout[currentIndex].reload));
    }
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
    [PunRPC]
    void PickupWeapon(string name)
    {
        //find weapon from a library
        Gun newWeapon = GunLibrary.FindGun(name);
        newWeapon.InitAmmo();
        if (loadout.Count >= 2)
        {
            if (newWeapon.name != loadout[1].name)
            {
                //replace weapon that we are holding
                loadout[1] = newWeapon;
                Equip(currentIndex);
            }
        }
        else
        {
            loadout.Add(newWeapon);
            Equip(loadout.Count - 1);
        }
        //add weapon to the loadout
    }
}
