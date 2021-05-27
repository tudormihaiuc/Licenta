using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using Photon.Realtime;
using UnityEngine.UI;

[System.Serializable]
public class ProfileData
{
    public string username;
    public int level;
    public int xp;

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

[System.Serializable]
public class MapData
{
    public string name;
    public int scene;
}
public class Launcher : MonoBehaviourPunCallbacks
{
    public static Launcher Instance;
    [SerializeField] TMP_InputField roomNameInputField;
    [SerializeField] InputField usernameField;
    [SerializeField] TMP_Text errorText;
    [SerializeField] TMP_Text roomNameText;
    [SerializeField] Transform roomListContent;
    [SerializeField] Transform playerListContent;
    [SerializeField] GameObject roomListItemPrefab;
    [SerializeField] GameObject PlayerListItemPrefab;

    [SerializeField] GameObject startGameButton;
    public Slider maxPlayersSlider;
    public TMP_Text maxPlayerValue;
    public int numOfPlayers = 10;

    //public InputField usernameField;
    public static ProfileData myProfile = new ProfileData();
    public TMP_Text mapValue;
    public MapData[] maps;
    private int currentMap = 0;
    public TMP_Text modeValue;

    void Awake()
    {
        Instance = this;
        myProfile = Data.LoadProfile();
        if (!string.IsNullOrEmpty(myProfile.username))
            usernameField.text = myProfile.username;

    }
    void Start()
    {
        Debug.Log("Connecting to Master");
        PhotonNetwork.ConnectUsingSettings();
    }
    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master");
        PhotonNetwork.JoinLobby();
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public override void OnJoinedLobby()
    {
        MenuManager.Instance.OpenMenu("title");
        Debug.Log("Joined Lobyy");
    }
    public void ChangeMap()
    {
        currentMap++;
        if (currentMap >= maps.Length)
        {
            currentMap = 0;
        }
        mapValue.text = "MAP: " + maps[currentMap].name.ToUpper();
    }

    public void CreateRoom()
    {
        VerifyUsername();
        RoomOptions options = new RoomOptions();
        options.MaxPlayers = (byte)maxPlayersSlider.value;
        options.CustomRoomPropertiesForLobby = new string[] { "map","mode" };
        ExitGames.Client.Photon.Hashtable properties = new ExitGames.Client.Photon.Hashtable();
        properties.Add("map", maps[currentMap].name);
        properties.Add("mode",GameSettings.GameMode);
        options.CustomRoomProperties = properties;
        if (string.IsNullOrEmpty(roomNameInputField.text))
        {
            return;
        }
        PhotonNetwork.CreateRoom(roomNameInputField.text, options);
        MenuManager.Instance.OpenMenu("loading");
    }
    public void ChangeMaxPlayersSlider(float t_value)
    {
        maxPlayerValue.text = Mathf.RoundToInt(t_value).ToString();
    }

    public override void OnJoinedRoom()
    {
        Data.SaveProfile(myProfile);
        MenuManager.Instance.OpenMenu("room");
        roomNameText.text = PhotonNetwork.CurrentRoom.Name;
        Player[] players = PhotonNetwork.PlayerList;
        foreach (Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }
        for (int i = 0; i < players.Length; i++)
        {
            Instantiate(PlayerListItemPrefab, playerListContent).GetComponent<PlayerListItem>().SetUp(players[i]);
        }
        startGameButton.SetActive(PhotonNetwork.IsMasterClient);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        startGameButton.SetActive(PhotonNetwork.IsMasterClient);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        errorText.text = "Room Creation Failed: " + message;
        MenuManager.Instance.OpenMenu("error");
    }
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        errorText.text = "Room is full!";
        MenuManager.Instance.OpenMenu("error");
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        MenuManager.Instance.OpenMenu("loading");
    }

    public override void OnLeftRoom()
    {
        MenuManager.Instance.OpenMenu("title");
    }

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

    public void JoinRoom(RoomInfo info)
    {
        if (info.MaxPlayers <= info.PlayerCount)
        {
            errorText.text = "Room is full!";
            MenuManager.Instance.OpenMenu("error");
            Debug.Log("room is full");
        }
        else
        {
            VerifyUsername();
            PhotonNetwork.JoinRoom(info.Name);
            MenuManager.Instance.OpenMenu("loading");
        }

    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Instantiate(PlayerListItemPrefab, playerListContent).GetComponent<PlayerListItem>().SetUp(newPlayer);
    }

    public void StartGame()
    {
        PhotonNetwork.LoadLevel(maps[currentMap].scene);
        PhotonNetwork.CurrentRoom.IsVisible = false;
        PhotonNetwork.CurrentRoom.IsOpen = false;
        VerifyUsername();
    }
    private void FixedUpdate()
    {
        //VerifyUsername();
    }

    private void VerifyUsername()
    {
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
    public void ChangeMode(){
        int newMode=(int)GameSettings.GameMode+1;
        if(newMode>=System.Enum.GetValues(typeof(GameMode)).Length){
            newMode=0;
        }
        GameSettings.GameMode=(GameMode)newMode;
        modeValue.text="MODE: "+System.Enum.GetName(typeof(GameMode),newMode);
        if(GameSettings.GameMode==GameMode.F4A){
            maxPlayersSlider.gameObject.SetActive(true);
        }else{
            maxPlayersSlider.gameObject.SetActive(false);
            maxPlayersSlider.value=1;
            maxPlayerValue.text="1";
        }
    }
}
