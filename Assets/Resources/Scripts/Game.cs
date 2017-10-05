using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Game : Photon.PunBehaviour
{
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
    private Dictionary<int, string> callsDict = new Dictionary<int, string>() {
            {0, ": Četiri trice!"},
            {1, ": Tri trice bez špadi!"},
            {2, ": Tri trice bez dinara!"},
            {3, ": Tri trice bez kupa!"},
            {4, ": Tri trice bez baštona!"},
            {5, ": Četiri dvice!"},
            {6, ": Tri dvice bez špadi!"},
            {7, ": Tri dvice bez dinara!"},
            {8, ": Tri dvice bez kupa!"},
            {9, ": Tri dvice bez baštona!"},
            {10, ": Četiri aša!"},
            {11, ": Tri aša bez špadi!"},
            {12, ": Tri aša bez dinara!"},
            {13, ": Tri aša bez kupa!"},
            {14, ": Tri aša bez baštona!"},
            {15, ": Napola kupa!"},
            {16, ": Napola dinara!"},
            {17, ": Napola špadi!"},
            {18, ": Napola baštona!"}
        };

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

    public Vector3 screenPoint;

    private void Shuffle(int[] deck)
    {
        for (int n = deck.Length - 1; n > 0; --n)
        {
            int k = r.Next(n + 1);
            int temp = deck[n];
            deck[n] = deck[k];
            deck[k] = temp;
        }
    }

    void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        loadSprites = Resources.LoadAll<Sprite>("Textures/cards");

        foreach (Sprite s in loadSprites)
        {
            cardDict.Add(s.ToString().Split(' ')[0], s);
        }

        System.Array.Sort(PhotonNetwork.playerList);

        foreach (PhotonPlayer p in PhotonNetwork.playerList)
        {
            if (PlayerPrefs.GetInt(p.NickName) == 1)
            {
                if (arrayPlayers[0].text == "Empty slot")
                    arrayPlayers[0].text = p.NickName;
                else
                    arrayPlayers[2].text = p.NickName;
            }
            else
            {
                if (arrayPlayers[1].text == "Empty slot")
                    arrayPlayers[1].text = p.NickName;
                else
                    arrayPlayers[3].text = p.NickName;
            }
        }

        if (PhotonNetwork.isMasterClient)
        {
            SetupDeck();

            photonView.RPC("RpcDealCards", PhotonTargets.All, rng);
        }

        if (PhotonNetwork.playerName == arrayPlayers[start].text)
            StartCoroutine(DelayStartTurn());
    }


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

    private void SetupDeck()
    {
        for (int i = 0; i < 40; i++)
            deck[i] = i;

        Shuffle(deck);
        Shuffle(deck);

        foreach (int i in deck)
            rng += i.ToString() + " ";
    }

    [PunRPC]
    private void RpcDealCards(string rng)
    {
        if (PhotonNetwork.playerName == arrayPlayers[0].text)
            DealCards(rng, 0);
        else if (PhotonNetwork.playerName == arrayPlayers[1].text)
            DealCards(rng, 10);
        else if (PhotonNetwork.playerName == arrayPlayers[2].text)
            DealCards(rng, 20);
        else if (PhotonNetwork.playerName == arrayPlayers[3].text)
            DealCards(rng, 30);
    }

    private void DealCards(string rng, int gap)
    {
        for (int i = 0; i < 10; i++)
            sortMe.Add(loadSprites[System.Int32.Parse(rng.Split(' ')[i + gap])]);

        sortMe = sortMe.OrderBy(p => p.ToString()[3]).ToList();

        //used for changing card positions faster
        /*
         		tr = loadCards [i].GetComponent<Transform> ();
				tr.position = new Vector2 (0, -3.9f);
                tr.RotateAround (new Vector3 (0f, -14.57f, 0f), Vector3.forward, -28 + i * 6);
         */
        for (int i = 0; i < 10; i++)
        {
            spriteRenderer = cardSlots[i].GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = sortMe[i];
            spriteRenderer.name = sortMe[i].name;

            cardSlots[i].GetComponent<BoxCollider2D>().enabled = false;
        }

        foreach (GameObject g in cardSlots)
        {
            callsList.Add(g.GetComponent<SpriteRenderer>().sprite.ToString().Split(' ')[0]);
        }
    }

    private void StartTurn(bool first, bool start)
    {
        foreach (GameObject g in cardSlots)
            g.GetComponent<BoxCollider2D>().enabled = true;

        string newInfo = "Current turn: " + PhotonNetwork.playerName;
        photonView.RPC("RpcChangeInfo", PhotonTargets.All, newInfo);

        if (first)
        {
            buttonHit.interactable = true;
            buttonFlip.interactable = true;
        }

        if (start)
            dropDownCalls.interactable = true;
    }

    [PunRPC]
    private void RpcChangeInfo(string text)
    {
        textInfo.text = text;
    }

    [PunRPC]
    private void RpcThrowCard(string player, string card)
    {
        turnCounter += 1;

        if (player == arrayPlayers[0].text)
        {
            thrownCards[0].sprite = cardDict[card];

            if (turnCounter != 5 && PhotonNetwork.playerName == arrayPlayers[1].text)
                StartTurn(false, call);
            if (turnCounter == 2)
                firstLetter = card[3].ToString();
        }
        else if (player == arrayPlayers[1].text)
        {
            thrownCards[1].sprite = cardDict[card];

            if (turnCounter != 5 && PhotonNetwork.playerName == arrayPlayers[2].text)
                StartTurn(false, call);
            if (turnCounter == 2)
                firstLetter = card[3].ToString();
        }
        else if (player == arrayPlayers[2].text)
        {
            thrownCards[2].sprite = cardDict[card];

            if (turnCounter != 5 && PhotonNetwork.playerName == arrayPlayers[3].text)
                StartTurn(false, call);
            if (turnCounter == 2)
                firstLetter = card[3].ToString();
        }
        else
        {
            thrownCards[3].sprite = cardDict[card];

            if (turnCounter != 5 && PhotonNetwork.playerName == arrayPlayers[0].text)
                StartTurn(false, call);
            if (turnCounter == 2)
                firstLetter = card[3].ToString();
        }
    }

    private IEnumerator Pause()
    {
        yield return new WaitForSeconds(3.5f);
        photonView.RPC("RpcFinishTurn", PhotonTargets.All);
    }

    [PunRPC]
    private void RpcFinishTurn()
    {
        turnCounter = 1;
        cycleCounter += 1;

        int bele = 0;
        print(firstLetter);

        temp.Clear();
        temp.Add(thrownCards[0].sprite.ToString().Substring(0, 4));
        temp.Add(thrownCards[1].sprite.ToString().Substring(0, 4));
        temp.Add(thrownCards[2].sprite.ToString().Substring(0, 4));
        temp.Add(thrownCards[3].sprite.ToString().Substring(0, 4));

        foreach (string s in temp)
        {
            int tmp = System.Int32.Parse(Regex.Replace(s, "[A-Za-z ]", ""));

            if (tmp > 7 && tmp != 101)
            {
                bele += 1;
            }
            else if (tmp == 101)
            {
                bele += 3;
            }
        }

        if (cycleCounter == 11)
        {
            bele += 3;
        }

        for (int i = 0; i < 4; i++)
        {
            if (temp[i].Contains(firstLetter))
            {
                continue;
            }
            else
            {
                temp.RemoveAt(i);
                temp.Insert(i, "0");
            }
        }
        for (int i = 0; i < 4; i++)
        {
            temp.Add(Regex.Replace(temp[0], "[A-Za-z ]", ""));
            temp.RemoveAt(0);
        }

        string max = temp.Max();

        if (max + firstLetter == thrownCards[0].sprite.ToString().Substring(0, 4))
        {
            roundTeam1 += bele;
            team1 += bele;
            if (PhotonNetwork.playerName == arrayPlayers[0].text)
                StartTurn(true, call);
        }
        else if (max + firstLetter == thrownCards[1].sprite.ToString().Substring(0, 4))
        {
            roundTeam2 += bele;
            team2 += bele;
            if (PhotonNetwork.playerName == arrayPlayers[1].text)
                StartTurn(true, call);
        }
        else if (max + firstLetter == thrownCards[2].sprite.ToString().Substring(0, 4))
        {
            roundTeam1 += bele;
            team1 += bele;
            if (PhotonNetwork.playerName == arrayPlayers[2].text)
                StartTurn(true, call);
        }
        else
        {
            roundTeam2 += bele;
            team2 += bele;
            if (PhotonNetwork.playerName == arrayPlayers[3].text)
                StartTurn(true, call);
        }

        thrownCards[0].sprite = Frame0;
        thrownCards[1].sprite = Frame0;
        thrownCards[2].sprite = Frame0;
        thrownCards[3].sprite = Frame0;
        textHF.text = "";

        if (cycleCounter == 11)
        {
            if (System.Math.Floor(team1 / 3) >= 41 || System.Math.Floor(team2 / 3) >= 41)
            {
                panelScore.SetActive(true);
                textScoreT1.text = System.Math.Floor(team1 / 3).ToString();
                textScoreT2.text = System.Math.Floor(team2 / 3).ToString();
                return;
            }

            round += 1;
            start += 1;

            if (start == 4)
                start = 0;

            textHF.text = string.Format("Round {0} Team 1 / Team 2 - {1} / {2}\nA new round will start shortly!",
                round, System.Math.Floor(roundTeam1 / 3).ToString(), System.Math.Floor(roundTeam2 / 3).ToString());
            StartCoroutine(Restart());
        }
    }

    private IEnumerator Restart()
    {
        yield return new WaitForSeconds(5f);
        roundTeam1 = 0;
        roundTeam2 = 0;
        turnCounter = 1;
        cycleCounter = 1;
        rng = "";
        call = true;
        callsList.Clear();
        sortMe.Clear();

        if (PhotonNetwork.isMasterClient)
        {
            SetupDeck();

            photonView.RPC("RpcDealCards", PhotonTargets.All, rng);
        }

        if (PhotonNetwork.playerName == arrayPlayers[start].text)
            StartCoroutine(DelayStartTurn());
    }

    public void Hit()
    {
        photonView.RPC("RpcHit", PhotonTargets.All, PhotonNetwork.playerName);
    }

    [PunRPC]
    private void RpcHit(string player)
    {
        textHF.text = player + ": Tučem!";
    }

    public void Flip()
    {
        photonView.RPC("RpcFlip", PhotonTargets.All, PhotonNetwork.playerName);
    }

    [PunRPC]
    private void RpcFlip(string player)
    {
        textHF.text = player + ": Strišo!";
    }

    public void Call(Dropdown target)
    {
        if (target.value == 1)
        {
            if (callsList.Contains("103k") && callsList.Contains("103b") && callsList.Contains("103d") && callsList.Contains("103s"))
            {
                photonView.RPC("RpcCall", PhotonTargets.All, 12, 0, PhotonNetwork.playerName);
            }
            else if (callsList.Contains("103k") && callsList.Contains("103b") && callsList.Contains("103d"))
            {
                photonView.RPC("RpcCall", PhotonTargets.All, 9, 1, PhotonNetwork.playerName);
            }
            else if (callsList.Contains("103k") && callsList.Contains("103b") && callsList.Contains("103s"))
            {
                photonView.RPC("RpcCall", PhotonTargets.All, 9, 2, PhotonNetwork.playerName);
            }
            else if (callsList.Contains("103s") && callsList.Contains("103b") && callsList.Contains("103d"))
            {
                photonView.RPC("RpcCall", PhotonTargets.All, 9, 3, PhotonNetwork.playerName);
            }
            else if (callsList.Contains("103d") && callsList.Contains("103s") && callsList.Contains("103k"))
            {
                photonView.RPC("RpcCall", PhotonTargets.All, 9, 4, PhotonNetwork.playerName);
            }
        }
        else if (target.value == 2)
        {
            if (callsList.Contains("102k") && callsList.Contains("102b") && callsList.Contains("102d") && callsList.Contains("102s"))
            {
                photonView.RPC("RpcCall", PhotonTargets.All, 12, 5, PhotonNetwork.playerName);
            }
            else if (callsList.Contains("102k") && callsList.Contains("102b") && callsList.Contains("102d"))
            {
                photonView.RPC("RpcCall", PhotonTargets.All, 9, 6, PhotonNetwork.playerName);
            }
            else if (callsList.Contains("102k") && callsList.Contains("102b") && callsList.Contains("102s"))
            {
                photonView.RPC("RpcCall", PhotonTargets.All, 9, 7, PhotonNetwork.playerName);
            }
            else if (callsList.Contains("102s") && callsList.Contains("102b") && callsList.Contains("102d"))
            {
                photonView.RPC("RpcCall", PhotonTargets.All, 9, 8, PhotonNetwork.playerName);
            }
            else if (callsList.Contains("102d") && callsList.Contains("102s") && callsList.Contains("102k"))
            {
                photonView.RPC("RpcCall", PhotonTargets.All, 9, 9, PhotonNetwork.playerName);
            }
        }
        else if (target.value == 3)
        {
            if (callsList.Contains("101k") && callsList.Contains("101b") && callsList.Contains("101d") && callsList.Contains("101s"))
            {
                photonView.RPC("RpcCall", PhotonTargets.All, 12, 10, PhotonNetwork.playerName);
            }
            else if (callsList.Contains("101k") && callsList.Contains("101b") && callsList.Contains("101d"))
            {
                photonView.RPC("RpcCall", PhotonTargets.All, 9, 11, PhotonNetwork.playerName);
            }
            else if (callsList.Contains("101k") && callsList.Contains("101b") && callsList.Contains("101s"))
            {
                photonView.RPC("RpcCall", PhotonTargets.All, 9, 12, PhotonNetwork.playerName);
            }
            else if (callsList.Contains("101s") && callsList.Contains("101b") && callsList.Contains("101d"))
            {
                photonView.RPC("RpcCall", PhotonTargets.All, 9, 13, PhotonNetwork.playerName);
            }
            else if (callsList.Contains("101d") && callsList.Contains("101s") && callsList.Contains("101k"))
            {
                photonView.RPC("RpcCall", PhotonTargets.All, 9, 14, PhotonNetwork.playerName);
            }
        }
        else if (target.value == 4)
        {
            if (callsList.Contains("101k") && callsList.Contains("102k") && callsList.Contains("103k"))
            {
                photonView.RPC("RpcCall", PhotonTargets.All, 9, 15, PhotonNetwork.playerName);
            }
            else if (callsList.Contains("101d") && callsList.Contains("102d") && callsList.Contains("103d"))
            {
                photonView.RPC("RpcCall", PhotonTargets.All, 9, 16, PhotonNetwork.playerName);
            }
            else if (callsList.Contains("101s") && callsList.Contains("102s") && callsList.Contains("103s"))
            {
                photonView.RPC("RpcCall", PhotonTargets.All, 9, 17, PhotonNetwork.playerName);
            }
            else if (callsList.Contains("101b") && callsList.Contains("102b") && callsList.Contains("103b"))
            {
                photonView.RPC("RpcCall", PhotonTargets.All, 9, 18, PhotonNetwork.playerName);
            }
        }
    }

    [PunRPC]
    void RpcCall(int points, int loc, string src)
    {
        if (src == arrayPlayers[0].text || src == arrayPlayers[2].text)
        {
            roundTeam1 += points;
        }
        else if (src == arrayPlayers[1].text || src == arrayPlayers[3].text)
        {
            roundTeam2 += points;
        }

        textHF.text = src + callsDict[loc];
    }

    public void Exit()
    {
        PhotonNetwork.LeaveRoom();
    }
    
    public void CancelExit()
    {
        panelMenu.SetActive(false);
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene(0);
    }

    public override void OnPhotonPlayerDisconnected(PhotonPlayer otherPlayer)
    {
        textInfo.text = otherPlayer.NickName + " disconnected! Exiting...";
        StartCoroutine(DelayExit());
    }

    private IEnumerator DelayExit()
    {
        yield return new WaitForSeconds(3f);
        PhotonNetwork.LeaveRoom();
    }

    private IEnumerator DelayStartTurn()
    {
        yield return new WaitForSeconds(0.25f);
        StartTurn(true, call);
    }
}
