using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;

//hendles the display of players in a room
public class PlayerListItem : MonoBehaviourPunCallbacks
{
    [SerializeField] TMP_Text text;
    Player player;

    //sets up the name for the player
    public void SetUp(Player _player){
        player=_player;
        text.text=_player.NickName;
        
    }

    //if the player leaves the room, destroy the player item prefab 
    public override void OnPlayerLeftRoom(Player otherPlayer){
        if(player==otherPlayer){
            Destroy(gameObject);
        }
    }

    public override void OnLeftRoom(){
        Destroy(gameObject);
    }
   
}
