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

//class keeping the current match info about the player
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
//keeps track of player stats, spawns the player, initializes the ui for the player etc.
public class PlayerManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    PhotonView photonView;
    public GameObject[] spawnLocations;//possible spawning loations for the players
    public string player_prefab_string;
    public GameObject player_prefab;
    public List<PlayerInfo> playerInfo = new List<PlayerInfo>();//list keeping the match info of each player
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
    //event codes for each event used to sync the data between all players
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
        spawnLocations = GameObject.FindGameObjectsWithTag("SpawnPoint");//find the spawn points
    }
    void Start()
    {
        ValidateConnection();
        InitializeUI();
        InitializeTimer();
        totalKills = 0;
        NewPlayer_S(Launcher.myProfile);//send a new player event for each player that joins the game
        CreateController();


    }
    //Initializes all Ui for the player
    private void InitializeUI()
    {
        uiMyDeaths = GameObject.Find("HUD/Stats/Deaths/Text").GetComponent<Text>();
        uiMyKills = GameObject.Find("HUD/Stats/Kills/Text").GetComponent<Text>();
        uiKilled = GameObject.Find("HUD/WhoKilledWho/Killed/Text").GetComponent<Text>();
        uiKiller = GameObject.Find("HUD/WhoKilledWho/Killer/Text").GetComponent<Text>();
        uiLeaderboard = GameObject.Find("HUD").transform.Find("Leaderboard").transform;
        uiEndgame = GameObject.Find("Canvas").transform.Find("End Game").transform;
        uiTimer = GameObject.Find("HUD/Timer/Text").GetComponent<Text>();
    }

    //refreshes the Ui of the match timer
    private void RefreshTimerUI()
    {
        string minutes = (currentMatchTime / 60).ToString("00");
        string seconds = (currentMatchTime % 60).ToString("00");
        uiTimer.text = $"{minutes}:{seconds}";
    }

    //starts the timer of the match on the Hosts client, all the other players have the time synced at his timer
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
        //if the player presses the Tab key, activate the leaderboard
        if (Input.GetKey(KeyCode.Tab))
        {
            Leaderboard(uiLeaderboard);
        }
        //if the player stops pressing the Tab key, deactivate the leaderboard
        else
        {
            uiLeaderboard.gameObject.SetActive(false);
        }
        ScoreCheck();//check for the endgame condition
    }
    //function that creates the leaderboard
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
        //display
        bool t_alternateColors = false;
        //foreach player in the game, instantiate a pleyer card in the leaderboard
        foreach (PlayerInfo p in sorted)
        {
            GameObject newcard = Instantiate(playercard, p_lb) as GameObject;
            if (t_alternateColors)
            {
                newcard.GetComponent<Image>().color = new Color32(0, 0, 0, 180);
            }
            t_alternateColors = !t_alternateColors;
            //get the player stats and put it in the player card
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
    //sorts the players after their kill counters
    private List<PlayerInfo> SortPlayers(List<PlayerInfo> p_info)
    {
        List<PlayerInfo> sorted = new List<PlayerInfo>();
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

    //checks if an event is happening and syncs it between all players
    public void OnEvent(EventData photonEvent)
    {
        // the eventcodes lower then 200 are reserved by photon
        if (photonEvent.Code >= 200) return;

        EventCodes e = (EventCodes)photonEvent.Code;
        object[] o = (object[])photonEvent.CustomData;//get the data of each event and wrap it in an array

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
            case EventCodes.NewMatch:
                NewMatch_R();
                break;
            case EventCodes.RefreshTimer:
                RefreshTimer_R(o);
                break;
        }
    }

    //func  tion that spawns a player at a random location from the spawnLocations array
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

    //function used for easier testing (starting the game from any scene loads the first scene, the menu)
    private void ValidateConnection()
    {
        if (PhotonNetwork.IsConnected) return;
        SceneManager.LoadScene(0);
    }

    //function that sends a new player event over the network
    public void NewPlayer_S(ProfileData p)
    {
        object[] package = new object[7];

        package[0] = p.username;
        package[1] = p.level;
        package[2] = p.xp;
        package[3] = PhotonNetwork.LocalPlayer.ActorNumber;
        package[4] = (short)0;//kills
        package[5] = (short)0;//deaths
        package[6]=CalculateTeam();
        Debug.Log("player sent: " + p.username);
        //raise the actual event
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.NewPlayer,//which event it is
            package,//the package we encapsulated the data
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },//set to send the event to teh host
                                                                            //(he will send it to the other players)
            new SendOptions { Reliability = true }
        );
    }

    //function that receives the new player event
    public void NewPlayer_R(object[] data)
    {
        //unpack the received data
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
        playerInfo.Add(p);//add the received player to the player list that has all of the current players in the match in it
        UpdatePlayers_S((int)state, playerInfo);//send another event to update the current players on all machines
    }

    //functions that raises an event to update the players on all machines
    public void UpdatePlayers_S(int state, List<PlayerInfo> info)
    {
        object[] package = new object[info.Count + 1];//make a new package of the size of our list
        package[0] = state;//the state of the game(running or ending)
        //same thing as before, but we make a piece array for every player and every piece we add to the package
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
        //raise the event
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.UpdatePlayers,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },//send the event to all clients
            new SendOptions { Reliability = true }
        );
    }

    //functions that receives the event to update the players on all machines
    public void UpdatePlayers_R(object[] data)
    {
        //repopulate the playerInfo list of players with the new received data
        state = (GameState)data[0];//the state of the game(running or ending)
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

    //function that takes care of sending the changes in the stats of each player by raising an event 
    public void ChangeStat_S(int actor, byte stat, byte amt)
    {
        //make a package that contains the actor number of the player, the stat(kill/death) and the amount that needs to be added
        object[] package = new object[] { actor, stat, amt };

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.ChangeStat,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );
    }

    //function that receives the data containg the player stats update
    public void ChangeStat_R(object[] data)
    {
        int actor = (int)data[0];
        byte stat = (byte)data[1];
        byte amt = (byte)data[2];
        //chech for changes for every player
        for (int i = 0; i < playerInfo.Count; i++)
        {
            //update only for the correct player
            if (playerInfo[i].actor == actor)
            {
                switch (stat)
                {
                    case 0: //kills
                        playerInfo[i].kills += amt;//update the kills of the player
                        totalKills++;
                        Debug.Log($"Player {playerInfo[i].profile.username} : kills = {playerInfo[i].kills}");
                        uiKiller.text = $"{playerInfo[i].profile.username}";//update the last kill ui
                        break;

                    case 1: //deaths
                        playerInfo[i].deaths += amt;//update the deaths of the player
                        totalDeaths++;
                        Debug.Log($"Player {playerInfo[i].profile.username} : deaths = {playerInfo[i].deaths}");
                        uiKilled.text = $"{playerInfo[i].profile.username}";//update the last kill ui
                        break;
                }
                //if the player made a kill or died
                if (playerInfo[i].profile.username == Launcher.myProfile.username)
                {
                    uiMyDeaths.text = $"{playerInfo[i].deaths} deaths";//update the deaths ui of the player
                    uiMyKills.text = $"{playerInfo[i].kills} kills";//update the kills ui of the player
                }
                if (uiLeaderboard.gameObject.activeSelf)
                {
                    Leaderboard(uiLeaderboard);//update the leaderboard
                }
                break;
            }
        }
        ScoreCheck();//check for the endgame condition
    }

    //if player leaves the room, load the menu scene
    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        SceneManager.LoadScene(0);
    }

    private void ScoreCheck()
    {
        Debug.Log("check score");
        bool detectWin = false;
        //chech for every player
        foreach (PlayerInfo p in playerInfo)
        {
            //if a player reaches 10 kills
            if (p.kills >= 10)
            {
                detectWin = true;
                Debug.Log("detected win");
                break;
            }
            //or if the player is the only left player in the lobby
            if(PhotonNetwork.CurrentRoom.PlayerCount==1 && GameSettings.GameMode==GameMode.F4A){
                detectWin = true;
                Debug.Log("all other players left=>WIN");
                break;
            }
        }
        //if we detect a win 
        if (detectWin)
        {
            if (PhotonNetwork.IsMasterClient && state != GameState.Ending)
            {
                //update for all players 
                UpdatePlayers_S((int)GameState.Ending, playerInfo);
            }
        }
    }

    //function that handles the end of the match
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
        }
        uiEndgame.gameObject.SetActive(true);//show the endgame ui
        Leaderboard(uiEndgame.Find("Leaderboard"));//show the leaderboard so all players can see their endgame stats
        StartCoroutine(End(6f));//keep the endgame screen for 6 seconds 
    }
    private IEnumerator End(float p_wait)
    {
        yield return new WaitForSeconds(p_wait);
        if (newMatch)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                NewMatch_S();//not used
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
        //if the match timer is up
        if (currentMatchTime <= 0)
        {
            //end the game
            timerCoroutine = null;
            UpdatePlayers_S((int)GameState.Ending, playerInfo);
        }
        else
        {
            //if not, recall the coroutine
            RefreshTimer_S();
            timerCoroutine = StartCoroutine(Timer());
        }
    }

    //used to sent the current match time to all the other players
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

    //used to receive the current match time
    public void RefreshTimer_R(object[] data)
    {
        currentMatchTime = (int)data[0];
        RefreshTimerUI();
    }

    //FROM HERE BELOW IS CODE THAT IS NOT CURRENTLY USED, MAYBE USED FOR FUTURE FEATURES

    //function that checks if the game is ending
    private void StateCheck()
    {
        if (state == GameState.Ending)
        {
            Endgame();
        }
    }

    //function to start the new match, never used
    public void NewMatch_S()
    {
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.NewMatch,
            null,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );
    }

    //function to start the new match, never used
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

    //function to calculate the team of a player, never used
    private bool CalculateTeam(){
        return false;
    }
}
