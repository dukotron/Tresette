using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Game : Photon.PunBehaviour
{
    public static Game Instance = null;
    public CardManager cardManager;

    [System.Serializable]
    public struct CardPlayer
    {
        public Text playerSlotText;
        public Image playerSlotImage;
        public string slotCardLetter;
        public int slotCardNumber;

        public void SetLetter()
        {
            slotCardLetter = playerSlotImage.sprite.ToString().Substring(3, 1);
        }

        public void SetNumber()
        {
            slotCardNumber = int.Parse(playerSlotImage.sprite.ToString().Substring(0, 3));
        }

        public string GetSlotCard()
        {
            return slotCardNumber.ToString() + slotCardLetter;
        }
    }

    public GameObject panelMenu;
    public GameObject panelScore;
    public Button buttonHit;
    public Button buttonFlip;
    public RawImage rawImgThrow;
    public Text textHF;
    public Text textInfo;
    public Text textScoreT1;
    public Text textScoreT2;
    public Sprite Frame0;
    public Dropdown dropDownCalls;

    public CardPlayer[] cardPlayers = new CardPlayer[4];
    public Dictionary<string, Sprite> cardStringToSprite = new Dictionary<string, Sprite>();
    public int turnCounter = 0;
    public string letterOfTurn;
    public float roundScore1 = 0;
    public float roundScore2 = 0;

    private int cycleCounter = 0;
    private int startingPlayer = 0;
    private float totalScore1 = 0;
    private float totalScore2 = 0;
    private int gameRound = 0;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        System.Array.Sort(PhotonNetwork.playerList);
        foreach (PhotonPlayer p in PhotonNetwork.playerList)
        {
            if (PlayerPrefs.GetInt(p.NickName) == 1)
            {
                if (cardPlayers[0].playerSlotText.text == "Empty slot")
                    cardPlayers[0].playerSlotText.text = p.NickName;
                else
                    cardPlayers[2].playerSlotText.text = p.NickName;
            }
            else
            {
                if (cardPlayers[1].playerSlotText.text == "Empty slot")
                    cardPlayers[1].playerSlotText.text = p.NickName;
                else
                    cardPlayers[3].playerSlotText.text = p.NickName;
            }
        }
        cardManager.Initialize();
        cardManager.DisableCardInteraction();

        if (PhotonNetwork.playerName == cardPlayers[startingPlayer].playerSlotText.text)
            StartCoroutine(DelayStartTurn());
    }

    void Update()
    {

    }

    public void StartTurn(bool first)
    {
        cardManager.EnableCardInteraction();

        string newInfo = "Current turn: " + PhotonNetwork.playerName;
        photonView.RPC("RpcChangeInfo", PhotonTargets.All, newInfo);

        if (first)
        {
            buttonHit.interactable = true;
            buttonFlip.interactable = true;
        }

        if (cycleCounter == 0)
            dropDownCalls.interactable = true;
    }

    public void FinishTurn()
    {
        StartCoroutine(Pause());
    }

    private IEnumerator Pause()
    {
        yield return new WaitForSeconds(3.5f);
        //photonView.RPC("RpcFinishTurn", PhotonTargets.All);
        RpcFinishTurn();
    }

    public void RpcFinishTurn()
    {
        turnCounter = 0;
        cycleCounter += 1;

        int bele = 0;
        int highestCard = 0;
        int winingPlayer = 0;

        for (int i = 0; i < cardPlayers.Length; i++)
        {
            cardPlayers[i].SetLetter();
            cardPlayers[i].SetNumber();
        }

        cardPlayers[0].playerSlotImage.sprite = Frame0;
        cardPlayers[1].playerSlotImage.sprite = Frame0;
        cardPlayers[2].playerSlotImage.sprite = Frame0;
        cardPlayers[3].playerSlotImage.sprite = Frame0;
        textHF.text = "";

        for (int i = 0; i < cardPlayers.Length; i++)
        {
            if (cardPlayers[i].slotCardNumber > 7 && cardPlayers[i].slotCardNumber != 101)
                bele += 1;
            else if (cardPlayers[i].slotCardNumber == 101)
                bele += 3;

            if (cardPlayers[i].slotCardNumber > highestCard && cardPlayers[i].slotCardLetter == letterOfTurn)
            {
                highestCard = cardPlayers[i].slotCardNumber;
                winingPlayer = i;
            }
        }

        if (cycleCounter == 10)
            bele += 3;

        if (winingPlayer == 0)
        {
            roundScore1 += bele;
            totalScore1 += bele;
            if (PhotonNetwork.playerName == cardPlayers[0].playerSlotText.text)
                StartTurn(true);
        }
        else if (winingPlayer == 1)
        {
            roundScore2 += bele;
            totalScore2 += bele;
            if (PhotonNetwork.playerName == cardPlayers[1].playerSlotText.text)
                StartTurn(true);
        }
        else if (winingPlayer == 2)
        {
            roundScore1 += bele;
            totalScore1 += bele;
            if (PhotonNetwork.playerName == cardPlayers[2].playerSlotText.text)
                StartTurn(true);
        }
        else
        {
            roundScore2 += bele;
            totalScore2 += bele;
            if (PhotonNetwork.playerName == cardPlayers[3].playerSlotText.text)
                StartTurn(true);
        }

        if (cycleCounter == 10)
        {
            if (System.Math.Floor(totalScore1 / 3) >= 41 || System.Math.Floor(totalScore2 / 3) >= 41)
            {
                panelScore.SetActive(true);
                textScoreT1.text = System.Math.Floor(totalScore1 / 3).ToString();
                textScoreT2.text = System.Math.Floor(totalScore2 / 3).ToString();
                return;
            }

            gameRound += 1;
            startingPlayer += 1;

            if (startingPlayer == 4)
                startingPlayer = 0;

            textHF.text = string.Format("Round {0} Team 1 / Team 2 - {1} / {2}\nA new round will start shortly!",
                gameRound, System.Math.Floor(roundScore1 / 3).ToString(), System.Math.Floor(roundScore2 / 3).ToString());
            StartCoroutine(Restart());
        }
    }

    private IEnumerator Restart()
    {
        yield return new WaitForSeconds(5f);
        roundScore1 = 0;
        roundScore2 = 0;
        turnCounter = 0;
        cycleCounter = 0;
        CardManager.rngSequence = "";
        cardStringToSprite = new Dictionary<string, Sprite>();

        cardManager.ClearCards();
        cardManager.Initialize();

        if (PhotonNetwork.playerName == cardPlayers[startingPlayer].playerSlotText.text)
            StartCoroutine(DelayStartTurn());
    }

    [PunRPC]
    private void RpcChangeInfo(string text)
    {
        textInfo.text = text;
    }

    [PunRPC]
    private void RpcHit(string player)
    {
        textHF.text = player + ": Tučem!";
    }

    [PunRPC]
    private void RpcFlip(string player)
    {
        textHF.text = player + ": Strišo!";
    }

    public void Hit()
    {
        photonView.RPC("RpcHit", PhotonTargets.All, PhotonNetwork.playerName);
    }

    public void Flip()
    {
        photonView.RPC("RpcFlip", PhotonTargets.All, PhotonNetwork.playerName);
    }

    public void CancelExit()
    {
        panelMenu.SetActive(false);
    }

    public void Exit()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        Destroy(gameObject);
        SceneManager.LoadScene(0);
    }

    public override void OnPhotonPlayerDisconnected(PhotonPlayer otherPlayer)
    {
        textInfo.text = otherPlayer.NickName + " disconnected! Exiting...";
        StartCoroutine(DelayExit());
    }

    private IEnumerator DelayStartTurn()
    {
        yield return new WaitForSeconds(0.25f);
        StartTurn(true);
    }

    private IEnumerator DelayExit()
    {
        yield return new WaitForSeconds(3f);
        PhotonNetwork.LeaveRoom();
    }
}
    /*public GameObject panelMenu;
    public GameObject panelScore;
    public Button buttonHit;
    public Button buttonFlip;
    public RawImage rawImgThrow;
    public Text textHF;
    public Text textInfo;
    public Text textScoreT1;
    public Text textScoreT2;
    public Sprite Frame0;
    public Dropdown dropDownCalls;

    public Text[] arrayPlayers = new Text[4];
    public int[] deck = new int[40];
    public Sprite[] loadSprites = new Sprite[40];
    public GameObject[] cardSlots = new GameObject[10];
    public Image[] thrownCards = new Image[4];
	
    private List<Sprite> sortMe = new List<Sprite>();
    private Dictionary<string, Sprite> cardDict = new Dictionary<string, Sprite>();
    private SpriteRenderer spriteRenderer;
    private string rng;
    private PhotonPlayer[] playerList = new PhotonPlayer[4];
    private GameObject dragMe;
    private Quaternion rot;
    private Vector3 SPos;
    private Vector2 touchPos, offset, GOcenter, newGOCenter;
    private bool draggingMode = false;
    private string myCard;
    private string firstLetter;
    private List<string> temp = new List<string>();
    private List<string> callsList = new List<string>();

    private int round = 0;
    private int turnCounter = 1;
    private int cycleCounter = 1;
    private float roundTeam1 = 0;
    private float roundTeam2 = 0;
    private float team1 = 0;
    private float team2 = 0;
    private int start = 0;
    private bool call = true;
    private System.Random r = new System.Random();


    void Update()
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            panelMenu.SetActive(true);
            panelScore.SetActive(false);
            foreach (GameObject g in cardSlots)
                g.GetComponent<SpriteRenderer>().color = new Color(38f, 38f, 38f, 1f);
        }

        if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics2D.Raycast(ray.origin, ray.direction))
            {
                dragMe = Physics2D.Raycast(ray.origin, ray.direction).collider.gameObject;
                rot = dragMe.transform.rotation;
                SPos = dragMe.transform.position;
                dragMe.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
                GOcenter = dragMe.transform.position;
                touchPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                offset = touchPos - GOcenter;
                draggingMode = true;
            }

            if (draggingMode)
            {
                if (dragMe.transform.position.y > 2)
                    rawImgThrow.texture = Resources.Load("Textures/checked") as Texture;
                else
                    rawImgThrow.texture = Resources.Load("Textures/unchecked") as Texture;
                touchPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                newGOCenter = touchPos - offset;
                dragMe.transform.position = new Vector2(newGOCenter.x, newGOCenter.y);
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            draggingMode = false;
            rawImgThrow.texture = Resources.Load("Textures/transparent") as Texture;
            if (dragMe.transform.position.y > 2)
            {
                if (dragMe.name == "Card")
                    return;

                myCard = dragMe.name;
                dragMe.GetComponent<SpriteRenderer>().sprite = null;
                dragMe.name = "Card";

                dropDownCalls.interactable = false;
                buttonHit.interactable = false;
                buttonFlip.interactable = false;
                call = false;
                foreach (GameObject g in cardSlots)
                    g.GetComponent<BoxCollider2D>().enabled = false;

                photonView.RPC("RpcThrowCard", PhotonTargets.All, PhotonNetwork.playerName, myCard);

                if (turnCounter == 5)
                {
                    StartCoroutine(Pause());
                }

                dragMe.transform.position = new Vector3(SPos.x, SPos.y, SPos.z);
                dragMe.transform.rotation = Quaternion.Euler(rot.eulerAngles.x, rot.eulerAngles.y, rot.eulerAngles.z);
            }
            else
            {
                dragMe.transform.position = new Vector3(SPos.x, SPos.y, SPos.z);
                dragMe.transform.rotation = Quaternion.Euler(rot.eulerAngles.x, rot.eulerAngles.y, rot.eulerAngles.z);
            }
        }
#else
        foreach (Touch touch in Input.touches)
        {
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    Ray ray = Camera.main.ScreenPointToRay(touch.position);

                    if (Physics2D.Raycast(ray.origin, ray.direction))
                    {
                        dragMe = Physics2D.Raycast(ray.origin, ray.direction).collider.gameObject;
                        rot = dragMe.transform.rotation;
                        SPos = dragMe.transform.position;
                        dragMe.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
                        GOcenter = dragMe.transform.position;
                        touchPos = Camera.main.ScreenToWorldPoint(touch.position);
                        offset = touchPos - GOcenter;
                        draggingMode = true;
                    }
                    break;

                case TouchPhase.Moved:
                    if (draggingMode)
                    {
                        if (dragMe.transform.position.y > 2)
                            rawImgThrow.texture = Resources.Load("Textures/checked") as Texture;
                        else
                            rawImgThrow.texture = Resources.Load("Textures/unchecked") as Texture;
                        touchPos = Camera.main.ScreenToWorldPoint(touch.position);
                        newGOCenter = touchPos - offset;
                        dragMe.transform.position = new Vector2(newGOCenter.x, newGOCenter.y);
                    }
                    break;

                case TouchPhase.Ended:
                    draggingMode = false;
                    rawImgThrow.texture = Resources.Load("Textures/transparent") as Texture;
                    if (dragMe.transform.position.y > 2)
                    {
                        if (dragMe.name == "Card")
                            return;

                        myCard = dragMe.name;
                        dragMe.GetComponent<SpriteRenderer>().sprite = null;
                        dragMe.name = "Card";

                        dropDownCalls.interactable = false;
                        buttonHit.interactable = false;
                        buttonFlip.interactable = false;
                        call = false;
                        foreach (GameObject g in cardSlots)
                            g.GetComponent<BoxCollider2D>().enabled = false;

                        photonView.RPC("RpcThrowCard", PhotonTargets.All, PhotonNetwork.playerName, myCard);

                        if (turnCounter == 5)
                        {
                            StartCoroutine(Pause());
                        }

                        dragMe.transform.position = new Vector3(SPos.x, SPos.y, SPos.z);
                        dragMe.transform.rotation = Quaternion.Euler(rot.eulerAngles.x, rot.eulerAngles.y, rot.eulerAngles.z);
                    }
                    else
                    {
                        dragMe.transform.position = new Vector3(SPos.x, SPos.y, SPos.z);
                        dragMe.transform.rotation = Quaternion.Euler(rot.eulerAngles.x, rot.eulerAngles.y, rot.eulerAngles.z);
                    }
                    break;
            }
        }
#endif
    }


}*/