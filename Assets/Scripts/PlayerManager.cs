using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.IO;
using UnityEngine.SceneManagement;
using ExitGames.Client.Photon;
using Photon.Realtime;
using UnityEngine.UI;
using System.Linq;

public class PlayerInfo
{
    public ProfileData profile;
    public int actor;
    public short kills;
    public short deaths;

    public PlayerInfo(ProfileData p, int a, short k, short d)
    {
        this.profile = p;
        this.actor = a;
        this.kills = k;
        this.deaths = d;
    }
}
public class PlayerManager : MonoBehaviour, IOnEventCallback
{
    PhotonView photonView;
    public GameObject[] spawnLocations;
    public List<PlayerInfo> playerInfo = new List<PlayerInfo>();
    public int myind;
    private Text uiMyKills;
    private Text uiMyDeaths;
    private Text uiKilled;
    private Text uiKiller;
    private Transform uiLeaderboard;
    public enum EventCodes : byte
    {
        NewPlayer,
        UpdatePlayers,
        ChangeStat
    }

    public void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    public void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }
    void Awake()
    {
        photonView = GetComponent<PhotonView>();
        spawnLocations = GameObject.FindGameObjectsWithTag("SpawnPoint");
    }
    void Start()
    {
        /*if(Camera.main){
            Camera.main.enabled=false;
        }*/
        ValidateConnection();
        InitializeUI();
        NewPlayer_S(Launcher.myProfile);
        if (photonView.IsMine)
        {
            CreateController();
        }
    }
    private void InitializeUI(){
        uiMyDeaths=GameObject.Find("HUD/Stats/Deaths/Text").GetComponent<Text>();
        uiMyKills=GameObject.Find("HUD/Stats/Kills/Text").GetComponent<Text>();
        uiKilled=GameObject.Find("HUD/WhoKilledWho/Killed/Text").GetComponent<Text>();
        uiKiller=GameObject.Find("HUD/WhoKilledWho/Killer/Text").GetComponent<Text>();
        uiLeaderboard=GameObject.Find("HUD").transform.Find("Leaderboard").transform;
        //RefreshMyStats();
    }
    private void Update() {
        if(Input.GetKey(KeyCode.Tab)){
                //Debug.Log("activate leaderboard");
                Leaderboard(uiLeaderboard);
            }else{
                uiLeaderboard.gameObject.SetActive(false);
            }
        
    }
    private void Leaderboard(Transform p_lb){
        Debug.Log("entered leaderboard function");
        //clean 
        for(int i=2;i<p_lb.childCount;i++){
            Destroy(p_lb.GetChild(i).gameObject);
        }
        //set map and mode
        p_lb.Find("Header/Mode").GetComponent<Text>().text="FREE FOR ALL";
        p_lb.Find("Header/Map").GetComponent<Text>().text="Arena";

        GameObject playercard=p_lb.GetChild(1).gameObject;
        playercard.SetActive(false);

        //sort
        List<PlayerInfo> sorted=SortPlayers(playerInfo);
        //Debug.Log(sorted.Count);
        //display
        bool t_alternateColors=false;
        foreach(PlayerInfo a in sorted){
            GameObject newcard=Instantiate(playercard,p_lb) as GameObject;
            if(t_alternateColors){
                newcard.GetComponent<Image>().color=new Color32(0,0,0,180);
            }
            t_alternateColors=!t_alternateColors;
            newcard.transform.Find("Level").GetComponent<Text>().text=a.profile.level.ToString("00");
            newcard.transform.Find("Username").GetComponent<Text>().text=a.profile.username;
            newcard.transform.Find("Score Value").GetComponent<Text>().text=(a.kills*100).ToString();
            newcard.transform.Find("Kills Value").GetComponent<Text>().text=a.kills.ToString();
            newcard.transform.Find("Deaths Value").GetComponent<Text>().text=a.deaths.ToString();
            newcard.SetActive(true);
        }
        //activate
        p_lb.gameObject.SetActive(true);
        
    }
    private List<PlayerInfo> SortPlayers(List<PlayerInfo> p_info){
        List<PlayerInfo> sorted=new List<PlayerInfo>();
        Debug.Log(p_info.Count);
        while(sorted.Count<p_info.Count){
            short highest=-1;
            PlayerInfo selection=p_info[0];
            //next highest player
            foreach(PlayerInfo a in p_info){
                if(sorted.Contains(a)){
                    continue;
                }
                if(a.kills>highest){
                    selection=a;
                    highest=a.kills;
                }
            }
            //add player
            sorted.Add(selection);
            Debug.Log("nr of elements: "+sorted.Count);
        }
        return sorted.Distinct().ToList<PlayerInfo>();
    }
    
    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code >= 200) return;

        EventCodes e = (EventCodes)photonEvent.Code;
        object[] o = (object[])photonEvent.CustomData;

        switch (e)
        {
            case EventCodes.NewPlayer:
                NewPlayer_R(o);
                break;

            case EventCodes.UpdatePlayers:
                UpdatePlayers_R(o);
                break;

            case EventCodes.ChangeStat:
                ChangeStat_R(o);
                break;
        }
    }

    public void CreateController()
    {
        //here we instantiate our player controller
        Debug.Log("Instantiated Player Controller");
        int spawn = Random.Range(0, spawnLocations.Length);
        PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "PlayerController"), spawnLocations[spawn].transform.position, spawnLocations[spawn].transform.rotation);
    }
    private void ValidateConnection()
    {
        if (PhotonNetwork.IsConnected) return;
        SceneManager.LoadScene(0);
    }

    public void NewPlayer_S(ProfileData p)
    {
        object[] package = new object[6];

        package[0] = p.username;
        package[1] = p.level;
        package[2] = p.xp;
        package[3] = PhotonNetwork.LocalPlayer.ActorNumber;
        package[4] = (short)0;
        package[5] = (short)0;

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.NewPlayer,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
            new SendOptions { Reliability = true }
        );
    }

    public void NewPlayer_R(object[] data)
    {
        PlayerInfo p = new PlayerInfo(
            new ProfileData(
                (string)data[0],
                (int)data[1],
                (int)data[2]
            ),
            (int)data[3],
            (short)data[4],
            (short)data[5]
        );

        playerInfo.Add(p);
        UpdatePlayers_S(playerInfo);
    }

    public void UpdatePlayers_S(List<PlayerInfo> info)
    {
        object[] package = new object[info.Count];
        for (int i = 0; i < info.Count; i++)
        {
            object[] piece = new object[6];

            piece[0] = info[i].profile.username;
            piece[1] = info[i].profile.level;
            piece[2] = info[i].profile.xp;
            piece[3] = info[i].actor;
            piece[4] = info[i].kills;
            piece[5] = info[i].deaths;

            package[i] = piece;
        }

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.UpdatePlayers,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );
    }

    public void UpdatePlayers_R(object[] data)
    {
        playerInfo = new List<PlayerInfo>();

        for (int i = 0; i < data.Length; i++)
        {
            object[] extract = (object[])data[i];

            PlayerInfo p = new PlayerInfo(
                new ProfileData(
                    (string)extract[0],
                    (int)extract[1],
                    (int)extract[2]
                ),
                (int)extract[3],
                (short)extract[4],
                (short)extract[5]
            );

            playerInfo.Add(p);
        }
    }

    public void ChangeStat_S(int actor, byte stat, byte amt)
    {
        object[] package = new object[] { actor, stat, amt };

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.ChangeStat,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );
    }

    public void ChangeStat_R(object[] data)
    {
        int actor = (int)data[0];
        byte stat = (byte)data[1];
        byte amt = (byte)data[2];

        for (int i = 0; i < playerInfo.Count; i++)
        {
            if (playerInfo[i].actor == actor)
            {
                Debug.Log(playerInfo[i].actor);
                switch (stat)
                {
                    case 0: //kills
                        playerInfo[i].kills += amt;
                        Debug.Log($"Player {playerInfo[i].profile.username} : kills = {playerInfo[i].kills}");
                        uiKiller.text=$"{playerInfo[i].profile.username}";
                        break;

                    case 1: //deaths
                        playerInfo[i].deaths += amt;
                        Debug.Log($"Player {playerInfo[i].profile.username} : deaths = {playerInfo[i].deaths}");
                        uiKilled.text=$"{playerInfo[i].profile.username}";
                        break;
                }
                if(playerInfo[i].profile.username==Launcher.myProfile.username){
                    //RefreshMyStats();
                    uiMyDeaths.text=$"{playerInfo[i].deaths} deaths";
                    uiMyKills.text=$"{playerInfo[i].kills} kills";
                }
                if(uiLeaderboard.gameObject.activeSelf){
                    Leaderboard(uiLeaderboard);
                }
                return;
            }
        }
    }
}
