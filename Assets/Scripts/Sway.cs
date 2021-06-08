using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

//handles the movement of the gun deppending on the player movement 
public class Sway : MonoBehaviourPunCallbacks
{
    public float intesnsity;//the ammount of weapon sway
    public float smooth;//var used for lerping between the positions of the sway, so the movement looks smooth
    public bool isMine;//var used to check if it is the player's own gun or not
    public AudioClip pistolReloadSound;
    public AudioClip pumpSound;
    public AudioSource sfx;
    public ParticleSystem muzzleFlash;//muzzle flash effect
    private Quaternion origin_rotation;//the initial "position" 
    //(rotation actually since the sway it is the rotation of the gun on its own axis)
    private void Start() {
        origin_rotation=transform.localRotation;
    }
   private void Update() {
       //update the gun position every frame
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

    //starts the effect
    [PunRPC]
    public void MuzzleFlashStart(){
        muzzleFlash.Play();
    }

    //disables the effect
    public void MuzzleFlashDisable(){
        muzzleFlash.enableEmission=false;
    }

    //enables the effect
    public void MuzzleFlashEnable(){
        muzzleFlash.enableEmission=true;
    }
}
