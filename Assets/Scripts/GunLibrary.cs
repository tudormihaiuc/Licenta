using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//class that keeps track of all guns in the game, used for the weapon pick up feature
public class GunLibrary : MonoBehaviour
{
    public Gun[] allGuns;
    public static Gun[] guns;
    private void Awake() {
        guns=allGuns;
    }
    //finds and returns the correct gun
    public static Gun FindGun(string name){
        foreach(Gun gun in guns){
            if(gun.name.Equals(name)){
                return gun;
            }
        }
        return guns[0];
    }
}
