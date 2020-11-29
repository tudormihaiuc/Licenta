using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Weapon : MonoBehaviourPunCallbacks
{
    public Gun[] loadout;
    public Transform weaponParent;

    private GameObject currentWeapon;
    private int currentIndex;

    public GameObject bulletholePrefab;
    public LayerMask canBeShot;
    public float currentCooldown;

    public Camera camera;
    //PhotonView PV;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void Awake() {
        //PV=GetComponent<PhotonView>();
    }
    // Update is called once per frame
    void Update()
    {
        if(!photonView.IsMine){
            return;
        }
        if(Input.GetKeyDown(KeyCode.Alpha1)){
            photonView.RPC("Equip",RpcTarget.All,0);
            //Equip(0);
        }
        if(currentWeapon!=null){
            Aim(Input.GetMouseButton(1));
            if(Input.GetMouseButtonDown(0)&&currentCooldown<=0){
                Vector3 firingSpot=camera.transform.position;
                Vector3 fireDirection=camera.transform.forward;
                //Shoot(firingSpot,fireDirection);
                photonView.RPC("Shoot",RpcTarget.All,firingSpot,fireDirection);
                //Shoot();
            }
            currentWeapon.transform.localPosition=Vector3.Lerp(currentWeapon.transform.localPosition,Vector3.zero,Time.deltaTime*4f);
            //cooldown
            if(currentCooldown>0){
                currentCooldown-=Time.deltaTime;
            }
            }
        
    }
    void Aim(bool p_isAiming){
        Transform t_anchor=currentWeapon.transform.Find("Anchor");
        Transform t_state_hip=currentWeapon.transform.Find("States/Hip");
        Transform t_state_ads=currentWeapon.transform.Find("States/ADS");
        if(p_isAiming){
            //aim
            t_anchor.position=Vector3.Lerp(t_anchor.position,t_state_ads.position,Time.deltaTime*loadout[currentIndex].aimSpeed);
        }else{
            //hip
             t_anchor.position=Vector3.Lerp(t_anchor.position,t_state_hip.position,Time.deltaTime*loadout[currentIndex].aimSpeed);
        }
    }
    [PunRPC]
    void Equip(int p_ind){
        if(currentWeapon!=null){
            Destroy(currentWeapon);
        }
        currentIndex=p_ind;
        GameObject t_newWeapon=Instantiate(loadout[p_ind].prefab,weaponParent.position,weaponParent.rotation,weaponParent) as GameObject;
        t_newWeapon.transform.localPosition=Vector3.zero;
        t_newWeapon.transform.localEulerAngles=Vector3.zero;
        t_newWeapon.GetComponent<Sway>().enabled=photonView.IsMine;
        currentWeapon=t_newWeapon;
    }
    [PunRPC]
    void Shoot(Vector3 firingPoint,Vector3 firingDirection){
        //Transform t_spawn=transform.Find("Cameras/NormalCamera");
        //bloom
        /*Vector3 t_bloom=t_spawn.position+t_spawn.forward*1000f;
        t_bloom+=Random.Range(-loadout[currentIndex].bloom,loadout[currentIndex].bloom)*t_spawn.up;
        t_bloom+=Random.Range(-loadout[currentIndex].bloom,loadout[currentIndex].bloom)*t_spawn.right;
        t_bloom-=t_spawn.position;
        t_bloom.Normalize();*/
        //raycast
        RaycastHit t_hit=new RaycastHit();
        
        Ray shooterRay=new Ray(firingPoint,firingDirection);
        if(Physics.Raycast(shooterRay,out t_hit,1000f,canBeShot)){
            GameObject t_newHole=Instantiate(bulletholePrefab,t_hit.point+t_hit.normal*0.001f,Quaternion.identity) as GameObject;
            t_newHole.transform.LookAt(t_hit.point+t_hit.normal);
            Destroy(t_newHole,5f);
            if(photonView.IsMine){
                //shooting a player
                if(t_hit.collider.gameObject.layer==11){
                    //RPC call to dmg player
                }
            }
        }
        //gun effects
        currentWeapon.transform.Rotate(-loadout[currentIndex].recoil,0,0);
        currentWeapon.transform.position-=currentWeapon.transform.forward*loadout[currentIndex].kickback;

        //rate of fire
        currentCooldown=loadout[currentIndex].firerate;
    }
}
