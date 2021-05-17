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
    public bool awayTeam;

    public PlayerInfo(ProfileData p, int a, short k, short d,bool t)
    {
        this.profile = p;
        this.actor = a;
        this.kills = k;
        this.deaths = d;
        this.awayTeam=t;
    }
}
public enum GameState
{
    Waiting = 0,
    Starting = 1,
    Playing = 2,
    Ending = 3
}
public class PlayerManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    PhotonView photonView;
    public GameObject[] spawnLocations;
    public string player_prefab_string;
    public GameObject player_prefab;
    public List<PlayerInfo> playerInfo = new List<PlayerInfo>();
    private Text uiMyKills;
    private Text uiMyDeaths;
    private Text uiKilled;
    private Text uiKiller;
    private Text uiTimer;
    private Transform uiLeaderboard;
    private Transform uiEndgame;
    private GameState state = GameState.Waiting;
    private bool newMatch=false;
    private int totalKills;
    private int totalDeaths;

    private int currentMatchTime;
    private Coroutine timerCoroutine;
    private int roundCount;
    private bool playerAdded;
    public enum EventCodes : byte
    {
        NewPlayer,
        UpdatePlayers,
        ChangeStat,
        NewMatch,
        RefreshTimer
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
        ValidateConnection();
        InitializeUI();
        InitializeTimer();
        totalKills = 0;
        NewPlayer_S(Launcher.myProfile);
        CreateController();


    }
    private void InitializeUI()
    {
        uiMyDeaths = GameObject.Find("HUD/Stats/Deaths/Text").GetComponent<Text>();
        uiMyKills = GameObject.Find("HUD/Stats/Kills/Text").GetComponent<Text>();
        uiKilled = GameObject.Find("HUD/WhoKilledWho/Killed/Text").GetComponent<Text>();
        uiKiller = GameObject.Find("HUD/WhoKilledWho/Killer/Text").GetComponent<Text>();
        uiLeaderboard = GameObject.Find("HUD").transform.Find("Leaderboard").transform;
        uiEndgame = GameObject.Find("Canvas").transform.Find("End Game").transform;
        uiTimer = GameObject.Find("HUD/Timer/Text").GetComponent<Text>();
        //RefreshMyStats();
    }
    private void RefreshTimerUI()
    {
        string minutes = (currentMatchTime / 60).ToString("00");
        string seconds = (currentMatchTime % 60).ToString("00");
        uiTimer.text = $"{minutes}:{seconds}";
    }
    private void InitializeTimer()
    {
        currentMatchTime = 180;
        RefreshTimerUI();
        if (PhotonNetwork.IsMasterClient)
        {
            timerCoroutine = StartCoroutine(Timer());
        }
    }
    private void Update()
    {
        if (state == GameState.Ending)
        {
            return;
        }
        if (Input.GetKey(KeyCode.Tab))
        {
            //Debug.Log("activate leaderboard");
            Leaderboard(uiLeaderboard);
        }
        else
        {
            uiLeaderboard.gameObject.SetActive(false);
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            foreach (PlayerInfo a in playerInfo)
            {
                Debug.Log(a.profile.username);
            }
        }
    }
    private void Leaderboard(Transform p_lb)
    {
        //clean 
        for (int i = 2; i < p_lb.childCount; i++)
        {
            Destroy(p_lb.GetChild(i).gameObject);
        }
        //set map and mode
        p_lb.Find("Header/Mode").GetComponent<Text>().text = System.Enum.GetName(typeof(GameMode),GameSettings.GameMode);
        p_lb.Find("Header/Map").GetComponent<Text>().text = SceneManager.GetActiveScene().name;

        GameObject playercard = p_lb.GetChild(1).gameObject;
        playercard.SetActive(false);

        //sort
        List<PlayerInfo> sorted = SortPlayers(playerInfo);
        //Debug.Log(sorted.Count);
        //display
        bool t_alternateColors = false;
        foreach (PlayerInfo p in sorted)
        {
            GameObject newcard = Instantiate(playercard, p_lb) as GameObject;
            if (t_alternateColors)
            {
                newcard.GetComponent<Image>().color = new Color32(0, 0, 0, 180);
            }
            t_alternateColors = !t_alternateColors;
            newcard.transform.Find("Level").GetComponent<Text>().text = p.profile.level.ToString("00");
            newcard.transform.Find("Username").GetComponent<Text>().text = p.profile.username;
            newcard.transform.Find("Score Value").GetComponent<Text>().text = (p.kills * 100).ToString();
            newcard.transform.Find("Kills Value").GetComponent<Text>().text = p.kills.ToString();
            newcard.transform.Find("Deaths Value").GetComponent<Text>().text = p.deaths.ToString();
            newcard.SetActive(true);
        }
        //activate
        p_lb.gameObject.SetActive(true);

    }
    private List<PlayerInfo> SortPlayers(List<PlayerInfo> p_info)
    {
        List<PlayerInfo> sorted = new List<PlayerInfo>();

        //Debug.Log(p_info.Count);
        while (sorted.Count < p_info.Count)
        {
            short highest = -1;
            PlayerInfo selection = p_info[0];
            //next highest player
            foreach (PlayerInfo a in p_info)
            {
                if (sorted.Contains(a))
                {
                    continue;
                }
                if (a.kills > highest)
                {
                    selection = a;
                    highest = a.kills;
                }
            }
            //add player
            sorted.Add(selection);
            Debug.Log("nr of elements: " + sorted.Count);
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
                //Debug.Log("new player");
                NewPlayer_R(o);
                break;

            case EventCodes.UpdatePlayers:
                UpdatePlayers_R(o);
                break;

            case EventCodes.ChangeStat:
                ChangeStat_R(o);
                break;
            case EventCodes.NewMatch:
                NewMatch_R();
                break;
            case EventCodes.RefreshTimer:
                RefreshTimer_R(o);
                break;
        }
    }

    public void CreateController()
    {
        //here we instantiate our player controller
        Debug.Log("Instantiated Player Controller");
        int spawn = Random.Range(0, spawnLocations.Length);
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Instantiate(player_prefab_string, spawnLocations[spawn].transform.position, spawnLocations[spawn].transform.rotation);
        }else{
            GameObject newPlayer=Instantiate(player_prefab, spawnLocations[spawn].transform.position, spawnLocations[spawn].transform.rotation) as GameObject;
        }

    }
    private void ValidateConnection()
    {
        if (PhotonNetwork.IsConnected) return;
        SceneManager.LoadScene(0);
    }

    public void NewPlayer_S(ProfileData p)
    {
        object[] package = new object[7];

        package[0] = p.username;
        package[1] = p.level;
        package[2] = p.xp;
        package[3] = PhotonNetwork.LocalPlayer.ActorNumber;
        package[4] = (short)0;
        package[5] = (short)0;
        package[6]=CalculateTeam();
        Debug.Log("player sent: " + p.username);
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
            (short)data[5],
            (bool)data[6]
        );
        playerInfo.Add(p);
        UpdatePlayers_S((int)state, playerInfo);
    }

    public void UpdatePlayers_S(int state, List<PlayerInfo> info)
    {
        object[] package = new object[info.Count + 1];
        package[0] = state;
        for (int i = 0; i < info.Count; i++)
        {
            object[] piece = new object[7];

            piece[0] = info[i].profile.username;
            piece[1] = info[i].profile.level;
            piece[2] = info[i].profile.xp;
            piece[3] = info[i].actor;
            piece[4] = info[i].kills;
            piece[5] = info[i].deaths;
            piece[6]=info[i].awayTeam;

            package[i + 1] = piece;
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
        state = (GameState)data[0];
        playerInfo = new List<PlayerInfo>();

        for (int i = 1; i < data.Length; i++)
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
                (short)extract[5],
                (bool)extract[6]
            );

            playerInfo.Add(p);
            
        }
        StateCheck();
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
                switch (stat)
                {
                    case 0: //kills
                        playerInfo[i].kills += amt;
                        totalKills++;
                        Debug.Log($"Player {playerInfo[i].profile.username} : kills = {playerInfo[i].kills}");
                        uiKiller.text = $"{playerInfo[i].profile.username}";
                        break;

                    case 1: //deaths
                        playerInfo[i].deaths += amt;
                        totalDeaths++;
                        Debug.Log($"Player {playerInfo[i].profile.username} : deaths = {playerInfo[i].deaths}");
                        uiKilled.text = $"{playerInfo[i].profile.username}";
                        break;
                }
                if (playerInfo[i].profile.username == Launcher.myProfile.username)
                {
                    //RefreshMyStats();
                    uiMyDeaths.text = $"{playerInfo[i].deaths} deaths";
                    uiMyKills.text = $"{playerInfo[i].kills} kills";
                }
                if (uiLeaderboard.gameObject.activeSelf)
                {
                    Leaderboard(uiLeaderboard);
                }
                break;
            }
        }
        ScoreCheck();
    }
    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        SceneManager.LoadScene(0);
    }

    private void ScoreCheck()
    {
        Debug.Log("check score");
        bool detectWin = false;
        foreach (PlayerInfo p in playerInfo)
        {
            if (p.kills >= 10)
            {
                detectWin = true;
                Debug.Log("detected win");
                break;
            }
        }
        if (detectWin)
        {
            if (PhotonNetwork.IsMasterClient && state != GameState.Ending)
            {
                UpdatePlayers_S((int)GameState.Ending, playerInfo);
            }
        }
    }
    private void Endgame()
    {
        state = GameState.Ending;
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }
        currentMatchTime = 0;
        RefreshTimerUI();
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.DestroyAll();
            if (!newMatch)
            {
                PhotonNetwork.CurrentRoom.IsVisible = false;
                PhotonNetwork.CurrentRoom.IsOpen = false;
            }
        }
        uiEndgame.gameObject.SetActive(true);
        Leaderboard(uiEndgame.Find("Leaderboard"));
        StartCoroutine(End(6f));
    }
    private IEnumerator End(float p_wait)
    {
        yield return new WaitForSeconds(p_wait);
        if (newMatch)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                NewMatch_S();
            }
        }
        else
        {
            PhotonNetwork.AutomaticallySyncScene = false;
            PhotonNetwork.LeaveRoom();
        }
    }
    private IEnumerator Timer()
    {
        yield return new WaitForSeconds(1f);
        currentMatchTime -= 1;
        if (currentMatchTime <= 0)
        {
            timerCoroutine = null;
            UpdatePlayers_S((int)GameState.Ending, playerInfo);
        }
        else
        {
            RefreshTimer_S();
            timerCoroutine = StartCoroutine(Timer());
        }
    }
    public void RefreshTimer_S()
    {
        object[] package = new object[] { currentMatchTime };
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.RefreshTimer,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );
    }
    public void RefreshTimer_R(object[] data)
    {
        currentMatchTime = (int)data[0];
        RefreshTimerUI();
    }
    private void StateCheck()
    {
        if (state == GameState.Ending)
        {
            Endgame();
        }
    }
    public void NewMatch_S()
    {
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.NewMatch,
            null,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );
    }
    public void NewMatch_R()
    {
        state = GameState.Waiting;
        uiEndgame.gameObject.SetActive(false);
        foreach (PlayerInfo p in playerInfo)
        {
            p.kills = 0;
            p.deaths = 0;
        }
        InitializeTimer();
        CreateController();
    }
    private bool CalculateTeam(){
        return false;
    }
}
