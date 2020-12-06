using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName="New Gun",menuName="Gun")]
public class Gun : ScriptableObject
{
    public string name;
    public GameObject prefab;
    public float firerate;
    public float aimSpeed;
    public float bloom;
    public float recoil;
    public float kickback;
    public int damage;
    public int ammo;
    public int clipSize;
    public int clip;//current bullets in gthe clip
    public int stash;//current ammo
    public float reload;

    public bool FireBullet(){
        if(clip>0){
            clip-=1;
            return true;
        }else{
            return false;
        }
    }
    public void ReloadGun(){
        stash+=clip;
        clip=Mathf.Min(clipSize,stash);
        stash-=clip;
    }
    public void InitAmmo(){
        stash=ammo;
        clip=clipSize;
    }
    public int GetStash(){
        return stash;
    }
    public int GetClip(){
        return clip;
    }
}
