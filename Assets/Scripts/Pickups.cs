using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//handles the rotation of the Health and ammo pickups
public class Pickups : MonoBehaviour
{
    public GameObject[] pickups;
    // Start is called before the first frame update
    void Update()
    {
        //for each pickup, every second rotate a nr of degrees 
        foreach(GameObject pickup in pickups){
            pickup.transform.Rotate(0,30*Time.deltaTime,0);
        }
    }
}
