using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.IO;

public class PlayerManager : MonoBehaviour
{
    PhotonView PhotonView;
    void Awake() {
        PhotonView=GetComponent<PhotonView>();
    }
    void Start()
    {
        if(PhotonView.IsMine){
            CreateController();
        }
    }

    void CreateController(){
        //here we instantiate our player controller
        Debug.Log("Instantiated Player Controller");
        PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs","PlayerController"),Vector3.zero,Quaternion.identity);
    }
}
