using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using System.IO;

public class RoomManager : MonoBehaviourPunCallbacks
{

    public static RoomManager Instance;
    public Launcher launcher;

    void Awake() {
        if(Instance)//checks if another RoomManager exists
        {
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
        if(scene.buildIndex==1 || scene.buildIndex==2 || scene.buildIndex==3){//here we will instanciate the PlayerManager prefab
            Destroy(Instance);
        }
    }
    
}
