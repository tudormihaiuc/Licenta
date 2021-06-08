using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using System.IO;

//manages the rooms
public class RoomManager : MonoBehaviourPunCallbacks
{

    public static RoomManager Instance;

    void Awake() {
        //checks if another RoomManager exists
        if(Instance)
        {
            //destroy it, only one room manager should exist so everything is synced
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        Instance=this;
    }

     public override void OnEnable() {
        base.OnEnable(); 
        SceneManager.sceneLoaded+=OnSceneLoaded;
    }

    public override void OnDisable() {
        base.OnDisable(); 
        SceneManager.sceneLoaded-=OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode){
        if(scene.buildIndex==1 || scene.buildIndex==2 || scene.buildIndex==3){
            Destroy(Instance);
        }
    }
    
}
