using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using Photon.Realtime;
using UnityEngine.UI;

//class that takes care of the player info
[System.Serializable]
public class ProfileData
{
    public string username;
    public int level;
    public int xp;

    //default constructor that handles the case of a player not choosing a username
    public ProfileData()
    {
        this.username = "GUEST USER";
        this.level = 0;
        this.xp = 0;
    }
    public ProfileData(string username, int level, int xp)
    {
        this.username = username;
        this.level = level;
        this.xp = xp;
    }
}

//keeps info about the map
[System.Serializable]
public class MapData
{
    public string name;
    public int scene;
}

//class that takes care of the menu of the game (connecting to the game, creating/joing/leaving rooms, starting the game, changeing map/mode/nrOfMaxPlayers is a room etc.)
public class Launcher : MonoBehaviourPunCallbacks
{
    [SerializeField] TMP_InputField roomNameInputField;//room name Input Field
    [SerializeField] InputField usernameField; //username Input Field
    [SerializeField] TMP_Text errorText; //text variable that will be set with different values depending of the error
    [SerializeField] TMP_Text roomNameText; //room name set in the Input Field
    [SerializeField] Transform roomListContent; //transforms for the RoomItem Prefab
    [SerializeField] Transform playerListContent; //transforms for the PlayerItems Prefab
    [SerializeField] GameObject roomListItemPrefab; //room prefabs for the Launcher Script to show in the FindRoom Menu
    [SerializeField] GameObject PlayerListItemPrefab; //player prefabs for the Launcher Script to show in the Room Menu
    [SerializeField] GameObject startGameButton; //the start game button (I need this to make the button appear only for the host of the room)

    public static Launcher Instance; 
    public Slider maxPlayersSlider; //slider for the maximum nr of players is a room
    public TMP_Text maxPlayerValue; //Text variable for the player to visualize the numerical value of the slider
    public int numOfPlayers = 10;
    public static ProfileData myProfile = new ProfileData();
    public TMP_Text mapValue; //displays the currentMap name
    public MapData[] maps; //array of maps
    public TMP_Text modeValue; //displays the current Game Mode name
    private int currentMap = 0; //currentMap index

    void Awake()
    {
        Instance = this;
        //load the player profile and loaded player username 
        myProfile = Data.LoadProfile();
        if (!string.IsNullOrEmpty(myProfile.username))
            usernameField.text = myProfile.username;

    }

    //connect with Photon default settings
    void Start()
    {
        Debug.Log("Connecting to Master");
        PhotonNetwork.ConnectUsingSettings();
    }

    //called when the player is connected to the master server
    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master");
        PhotonNetwork.JoinLobby();
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    //when the the player connects, opens the TitleScreen of the menu
    public override void OnJoinedLobby()
    {
        MenuManager.Instance.OpenMenu("title");
        Debug.Log("Joined Lobby");
    }

    //Function that cycles betwwen the maps and sets the mapValue to the current map
    public void ChangeMap()
    {
        currentMap++;
        if (currentMap >= maps.Length)
        {
            currentMap = 0;
        }
        mapValue.text = "MAP: " + maps[currentMap].name.ToUpper();
    }

    //hendles the creation of Lobbies
    public void CreateRoom()
    {
        VerifyUsername();
        //creates new options for the current room
        RoomOptions options = new RoomOptions();
        options.MaxPlayers = (byte)maxPlayersSlider.value;//sets the maximum nr of players
        options.CustomRoomPropertiesForLobby = new string[] { "map","mode" };//creates custom properties to the room (map and game mode)
        ExitGames.Client.Photon.Hashtable properties = new ExitGames.Client.Photon.Hashtable();
        //adds the custom properties to the options of the room
        properties.Add("map", maps[currentMap].name);
        properties.Add("mode",GameSettings.GameMode);
        options.CustomRoomProperties = properties;
        //if the player did not set the name of the room, don't let him create it
        if (string.IsNullOrEmpty(roomNameInputField.text))
        {
            return;
        }
        PhotonNetwork.CreateRoom(roomNameInputField.text, options);
        MenuManager.Instance.OpenMenu("loading");
    }

    //handles the maxPlater slider
    public void ChangeMaxPlayersSlider(float t_value)
    {
        maxPlayerValue.text = Mathf.RoundToInt(t_value).ToString();
    }

    //handles the actions needed to be made after joining a room
    public override void OnJoinedRoom()
    {
        Data.SaveProfile(myProfile);//save the profile of the player again, just to be sure
        MenuManager.Instance.OpenMenu("room");//open the Room Menu
        roomNameText.text = PhotonNetwork.CurrentRoom.Name;//set the room name 
        Player[] players = PhotonNetwork.PlayerList;
        //clear the room player content before showing the refeshed values
        foreach (Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }
        //for each player display his name found in the player Item Prefab
        for (int i = 0; i < players.Length; i++)
        {
            Instantiate(PlayerListItemPrefab, playerListContent).GetComponent<PlayerListItem>().SetUp(players[i]);
        }
        //set the start game button active only for the host
        startGameButton.SetActive(PhotonNetwork.IsMasterClient);
    }

    //if the Host leaves, migrate the host to the next player
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        startGameButton.SetActive(PhotonNetwork.IsMasterClient);//set once again the start button for the new host
    }

    //sets the error text appropriately and opens the error menu
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        errorText.text = "Room Creation Failed: " + message;
        MenuManager.Instance.OpenMenu("error");
    }

    //sets the error text appropriately and opens the error menu
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        errorText.text = "Room is full!";
        MenuManager.Instance.OpenMenu("error");
    }

    //handles leaving the room
    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        MenuManager.Instance.OpenMenu("loading");
    }

    //if client left the room, open the Title Menu
    public override void OnLeftRoom()
    {
        MenuManager.Instance.OpenMenu("title");
    }

    //handles the updates of the current opened rooms
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (Transform trans in roomListContent)
        {
            Destroy(trans.gameObject);
        }
        for (int i = 0; i < roomList.Count; i++)
        {
            if (roomList[i].RemovedFromList)
            {
                continue;
            }
            Instantiate(roomListItemPrefab, roomListContent).GetComponent<RoomListItem>().SetUp(roomList[i]);
        }
    }

    //Handles joining a room  
    public void JoinRoom(RoomInfo info)
    {
        //lets players join only if the maxPlyer value is not exceded
        if (info.MaxPlayers <= info.PlayerCount)
        {
            //if it is, display an error msg
            errorText.text = "Room is full!";
            MenuManager.Instance.OpenMenu("error");
            Debug.Log("room is full");
        }
        else
        {
            //else, join
            VerifyUsername();
            PhotonNetwork.JoinRoom(info.Name);
            MenuManager.Instance.OpenMenu("loading");
        }

    }

    //if a new player join the room, display him in the room menu
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Instantiate(PlayerListItemPrefab, playerListContent).GetComponent<PlayerListItem>().SetUp(newPlayer);
    }

    //load the scene coresponding to the chosen map
    public void StartGame()
    {
        if(PhotonNetwork.CurrentRoom.CustomProperties["mode"]=="0"){
            GameSettings.GameMode=GameMode.F4A;
        }else{
            GameSettings.GameMode=GameMode.SOLO;
        }
        PhotonNetwork.LoadLevel(maps[currentMap].scene);
        //if the game has started, make the room invisible, so other players can't see it
        PhotonNetwork.CurrentRoom.IsVisible = false;
        //also, close the room, so other players can't join the game in progress
        PhotonNetwork.CurrentRoom.IsOpen = false;
        VerifyUsername();
    }

    //verifies the username Input field
    private void VerifyUsername()
    {
        //if it is empty, assign him a random name
        if (string.IsNullOrEmpty(usernameField.text))
        {
            myProfile.username = "Player" + Random.Range(0, 100).ToString("000");
            PhotonNetwork.NickName = "Player" + Random.Range(0, 100).ToString("000");
        }
        else
        {
            myProfile.username = usernameField.text;
            PhotonNetwork.NickName = usernameField.text;
        }
    }

    //hendles changing the game mode
    public void ChangeMode(){
        //cycle between game modes
        int newMode=(int)GameSettings.GameMode+1;
        if(newMode>=System.Enum.GetValues(typeof(GameMode)).Length){
            newMode=0;
        }
        GameSettings.GameMode=(GameMode)newMode;
        //display the current game mode
        modeValue.text="MODE: "+System.Enum.GetName(typeof(GameMode),newMode);
        //if the current game mode is free for all, let the player change the maxPlyerSlider
        if(GameSettings.GameMode==GameMode.F4A){
            maxPlayersSlider.gameObject.SetActive(true);
        }else{
            //else, don't let him change it, the game mode being single player vs Ai
            maxPlayersSlider.gameObject.SetActive(false);
            //automatically change the maxPlyerCount to only one player
            maxPlayersSlider.value=1;
            maxPlayerValue.text="1";
        }
    }
}
