using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Sway : MonoBehaviourPunCallbacks
{
   public float intesnsity;
   public float smooth;
   private Quaternion origin_rotation;
   public bool isMine;
   public AudioClip pistolReloadSound;
    public AudioClip pumpSound;
    public AudioSource sfx;
    public ParticleSystem muzzleFlash;

    private void Start() {
        origin_rotation=transform.localRotation;
        //MuzzleFlashStart();
    }
   private void Update() {
       UpdateSway();
       if(Pause.paused){
           return;
       }
   }

   private void UpdateSway(){
       float t_x_mouse=Input.GetAxis("Mouse X");
       float t_y_mouse=Input.GetAxis("Mouse Y");
       if(!isMine){
           t_x_mouse=0;
           t_y_mouse=0;
                  }

       //calculate target rotation
       Quaternion t_x_adj=Quaternion.AngleAxis(-1*intesnsity*t_x_mouse,Vector3.up);
       Quaternion t_y_adj=Quaternion.AngleAxis(intesnsity*t_y_mouse,Vector3.right);
       Quaternion target_rotation=origin_rotation*t_x_adj*t_y_adj;
       //rotate towards target rotation
       transform.localRotation=Quaternion.Lerp(transform.localRotation,target_rotation,Time.deltaTime*smooth);
   }
   public void PlayPistolReloadSound(){
        sfx.PlayOneShot(pistolReloadSound);
    }
    public void PlayPumpSound(){
        sfx.PlayOneShot(pumpSound);
    }
    [PunRPC]
    public void MuzzleFlashStart(){
        muzzleFlash.Play();
    }

    public void MuzzleFlashDisable(){
        muzzleFlash.enableEmission=false;
    }
    public void MuzzleFlashEnable(){
        muzzleFlash.enableEmission=true;
    }
}
