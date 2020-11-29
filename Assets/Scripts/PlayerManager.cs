using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.IO;

public class PlayerManager : MonoBehaviour
{
    PhotonView photonView;
    public GameObject[] spawnLocations;
    void Awake() {
        photonView=GetComponent<PhotonView>();
        spawnLocations=GameObject.FindGameObjectsWithTag("SpawnPoint");
    }
    void Start()
    {
        /*if(Camera.main){
            Camera.main.enabled=false;
        }*/
        if(photonView.IsMine){
            CreateController();
        }
    }

    void CreateController(){
        //here we instantiate our player controller
        Debug.Log("Instantiated Player Controller");
        int spawn=Random.Range(0,spawnLocations.Length);
        PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs","PlayerController"),spawnLocations[spawn].transform.position,Quaternion.identity);
    }
}
