using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//used to create a new Gun File streight from the Unity Editor
[CreateAssetMenu(fileName="New Gun",menuName="Gun")]

//class that keeps info about a certain gun
public class Gun : ScriptableObject
{
    public string name;//name of the gun
    public GameObject prefab;//prefab of a gun (gun + hands)
    public GameObject display;// display of a gun (for the weapon pickups)
    public float firerate;
    public float aimSpeed;//how fast the gun switches from the hip fire state to the Aim down sites state
    public float bloom;//bullet spread
    public float recoil;//how much the gun goes up when you shoot
    public float kickback;//how much the gun goes back when you shoot
    public int damage;
    public int ammo;
    public int clipSize;//the size of the gun mag
    public int clip;//current bullets in the clip
    public int stash;//current ammo
    public float reload;//reload duration
    //[Range(0,1)] public float mainFOV;
    //[Range(0,1)] public float weaponFOV;

    public int burst; // 0=semi, 1=auto, 2+=burst
    public AudioClip gunshotSound;
    public float soundRandomization;
    public float gunVolume;
    public int pellets;//nr of pallets the shotgun should fire
    public bool recovery;//var used for the check if the current gun is the shotgun

    //whenever the player shoots, substract a bullet from the clip
    public bool FireBullet(){
        if(clip>0){
            clip-=1;
            return true;//returning true means that the gun still has bullets in the mag, so the player can shoot
        }else{
            return false;//returning false means that the clip is empty and the players needs to reload 
        }
    }

    //refills the clip from the ammo stash
    public void ReloadGun(){
        stash+=clip;
        clip=Mathf.Min(clipSize,stash);
        stash-=clip;//subtract the reloaded bullets from the ammo stash
    }

    //initializes the starting ammo stash and bullets in the mag
    public void InitAmmo(){
        stash=ammo;
        clip=clipSize;
    }
    
    //much like the InitAmmo function, but only refreshes the ammo stash(used for the ammo pickup)
    public void InitAmmoWithoutReloading(){
        stash=ammo;
    }
    public int GetStash(){
        return stash;
    }
    public int GetClip(){
        return clip;
    }
    public int GetClipSize(){
        return clipSize;
    }
}
