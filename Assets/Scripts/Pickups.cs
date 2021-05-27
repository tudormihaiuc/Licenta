using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pickups : MonoBehaviour
{
    public GameObject[] pickups;
    // Start is called before the first frame update
    void Update()
    {
        foreach(GameObject pickup in pickups){
            pickup.transform.Rotate(0,30*Time.deltaTime,0);
        }
    }
}
