using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : Photon.PunBehaviour
{
    public GameObject panelLoading;
    public GameObject panelLogin;
    public GameObject panelMenu;
    public GameObject panelRoom;
    public GameObject panelInitial;
    public GameObject panelSecondary;
    public InputField inputFieldUsername;
    public InputField inputFieldRoom;
    public Text textInfoLogin;
    public Text textInfoJoinRoom;
    public Text textInfoRoom;
    public Text textVersion;

    public bool autoJoinLobby;
    public bool autoSyncScene;
    public string gameVersion;
    public Text[] arrayPlayers = new Text[4];
    public Text[] arrayTeams = new Text[4];

    private const string usernameError = "Username can't be empty!";
    private const string connectError = "Something went wrong while connecting to the server. Please try again.";
    private const string dcError = "Lost connection to the server...";
    private const string roomNameError = "Room can't be empty!";

    void Awake()
    {
        PhotonNetwork.autoJoinLobby = autoJoinLobby;
        PhotonNetwork.automaticallySyncScene = autoSyncScene;
        PhotonNetwork.sendRate = 20;
        PhotonNetwork.sendRateOnSerialize = 20;

        PhotonNetwork.networkingPeer.DebugOut = ExitGames.Client.Photon.DebugLevel.WARNING;
        PhotonNetwork.logLevel = PhotonLogLevel.ErrorsOnly;

    }

    void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        textVersion.text = "Version " + gameVersion;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            if (panelRoom.activeInHierarchy)
            {
                PhotonNetwork.LeaveRoom();
                panelRoom.SetActive(false);
                panelMenu.SetActive(true);
            }
        }
    }

    public void Connect()
    {
        if (PhotonNetwork.connected)
        {
            panelLogin.SetActive(false);
            panelMenu.SetActive(true);
            return;
        }

        if (inputFieldUsername.text == "")
        {
            textInfoLogin.text = usernameError;
            return;
        }

        panelLoading.SetActive(true);
        PhotonNetwork.AuthValues = new AuthenticationValues();
        PhotonNetwork.AuthValues.AuthType = CustomAuthenticationType.None;
        PhotonNetwork.ConnectUsingSettings(gameVersion);
    }

    public void GoTo22Full()
    {
        if (inputFieldRoom.text == "")
        {
            textInfoJoinRoom.text = roomNameError;
            return;
        }

        panelLoading.SetActive(true);
        PhotonNetwork.JoinRoom(inputFieldRoom.text);
    }

    public void SelectTeam1()
    {
        photonView.RPC("RpcSelectTeam1", PhotonTargets.All, PhotonNetwork.playerName);
    }

    public void SelectTeam2()
    {
        photonView.RPC("RpcSelectTeam2", PhotonTargets.All, PhotonNetwork.playerName);
    }

    [PunRPC]
    private void RpcSelectTeam1(string player)
    {
        ClearSelection(player);

        if (arrayTeams[0].text == "Empty slot")
        {
            arrayTeams[0].text = player;
        }
        else
        {
            arrayTeams[1].text = player; ;
        }

        SetTeam(player, 1);
        CheckTeams();
    }

    [PunRPC]
    private void RpcSelectTeam2(string player)
    {
        ClearSelection(player);

        if (arrayTeams[2].text == "Empty slot")
        {
            arrayTeams[2].text = player;
        }
        else
        {
            arrayTeams[3].text = player;
        }

        SetTeam(player, 2);
        CheckTeams();
    }

    private void ClearSelection(string player)
    {
        foreach (Text t in arrayTeams)
            if (t.text == player)
                t.text = "Empty slot";
    }

    private void SetTeam(string player, int team)
    {
        PlayerPrefs.SetInt(player, team);
    }

    private void CheckTeams()
    {
        if (arrayTeams[0].text != "Empty slot" && arrayTeams[1].text != "Empty slot" && 
            arrayTeams[2].text != "Empty slot" && arrayTeams[3].text != "Empty slot")
        {
            textInfoRoom.text = "Game will start shortly...";
            StartCoroutine(StartGame());
        }
    }

    private IEnumerator StartGame()
    {
        yield return new WaitForSeconds(3f);
        SceneManager.LoadScene(1);
    }

    public override void OnConnectedToMaster()
    {
        panelLoading.SetActive(false);
        panelLogin.SetActive(false);
        panelMenu.SetActive(true);
    }

    public override void OnFailedToConnectToPhoton(DisconnectCause cause)
    {
        textInfoLogin.text = connectError;
    }

    public override void OnDisconnectedFromPhoton()
    {
        panelLogin.SetActive(true);
        panelRoom.SetActive(false);
        panelMenu.SetActive(false);
        panelLoading.SetActive(false);
        textInfoLogin.text = dcError;
    }

    public override void OnJoinedRoom()
    {
        panelLoading.SetActive(false);
        panelMenu.SetActive(false);
        panelRoom.SetActive(true);
        panelInitial.SetActive(true);
        panelSecondary.SetActive(false);

        if (PhotonNetwork.isMasterClient)
            arrayPlayers[0].text = PhotonNetwork.playerName;
        else
        {
            int i = 0;
            foreach (PhotonPlayer p in PhotonNetwork.playerList)
            {
                print(p.NickName);
                arrayPlayers[i].text = p.NickName;
                i++;
            }
        }

        if (PhotonNetwork.room.PlayerCount == 4)
        {
            panelSecondary.SetActive(true);
            textInfoRoom.text = "Waiting for players to select a team...";
        }
    }

    public override void OnLeftRoom()
    {
        foreach (Text t in arrayPlayers)
            t.text = "Empty slot";
        foreach (Text t in arrayTeams)
            t.text = "Empty slot";
        textInfoRoom.text = "Waiting for players...";
    }

    public override void OnPhotonJoinRoomFailed(object[] codeAndMsg)
    {
        PhotonNetwork.CreateRoom(inputFieldRoom.text, new RoomOptions() { MaxPlayers = (byte) 4 }, null);
    }

    public override void OnPhotonPlayerConnected(PhotonPlayer other)
    {
        print(other.NickName);
        foreach (Text t in arrayPlayers)
        {
            if (t.text == "Empty slot")
            {
                t.text = other.NickName;
                break;
            }
        }

        if (PhotonNetwork.room.PlayerCount == 4)
        {
            panelSecondary.SetActive(true);
            textInfoRoom.text = "Waiting for players to select a team...";
        }
    }

    public override void OnPhotonPlayerDisconnected(PhotonPlayer other)
    {
        foreach (Text t in arrayPlayers)
            t.text = "Empty slot";

        int i = 0;
        foreach (PhotonPlayer p in PhotonNetwork.playerList)
        {
            arrayPlayers[i].text = p.NickName;
            i++;
        }

        foreach (Text t in arrayTeams)
            if (t.text == other.NickName)
                t.text = "Empty slot";

        textInfoRoom.text = "Waiting for players...";
        panelSecondary.SetActive(false);
    }
}