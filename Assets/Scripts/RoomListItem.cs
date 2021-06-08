using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using TMPro;
using Photon.Pun;

//handles the dispay the active rooms in the Find Room menu
public class RoomListItem : MonoBehaviour
{
    [SerializeField] TMP_Text text;//name of the room
    [SerializeField] TMP_Text text2;//player count
    [SerializeField] TMP_Text text3;//map name
    [SerializeField] TMP_Text text4;//game mode
    public RoomInfo info;

    //sets up the room with the given info
    public void SetUp(RoomInfo _info)
    {

        info = _info;
        text.text = _info.Name;
        text2.text = _info.PlayerCount + " / " + _info.MaxPlayers;
        text3.text = "Map: " + _info.CustomProperties["map"].ToString();
        if ((int)_info.CustomProperties["mode"] == 0)
        {
            text4.text = "F4A";
        }
        else
        {
            text4.text = "SOLO";
        }
        Debug.Log(_info.PlayerCount);
    }

    public void OnClick()
    {
        Launcher.Instance.JoinRoom(info);
    }
}
