using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using TMPro;
using Photon.Pun;

public class RoomListItem : MonoBehaviour
{
    [SerializeField] TMP_Text text;
    [SerializeField] TMP_Text text2;
    [SerializeField] TMP_Text text3;
    [SerializeField] TMP_Text text4;
    public RoomInfo info;
    public void SetUp(RoomInfo _info){
        
        info=_info;
        text.text=_info.Name;
        text2.text=_info.PlayerCount+" / "+_info.MaxPlayers;
        text3.text="Map: "+_info.CustomProperties["map"].ToString();
        text4.text=_info.CustomProperties["mode"].ToString();
        Debug.Log(_info.PlayerCount);
    }

    public void OnClick(){
        Launcher.Instance.JoinRoom(info);
    }
}
