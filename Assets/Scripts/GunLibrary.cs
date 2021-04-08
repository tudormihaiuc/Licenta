using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunLibrary : MonoBehaviour
{
    public Gun[] allGuns;
    public static Gun[] guns;
    private void Awake() {
        guns=allGuns;
    }
    public static Gun FindGun(string name){
        foreach(Gun gun in guns){
            if(gun.name.Equals(name)){
                return gun;
            }
        }
        return guns[0];
    }
}
