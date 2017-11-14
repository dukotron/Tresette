using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardManager : Photon.PunBehaviour, IPointerEnterHandler, IPointerExitHandler
{

    public static List<Card> activeCards;
    public static List<Card> inactiveCards;
    public static bool isCardDragged = false;
    public static Card cardDragged = null;
    public static bool inManager = true;
    public static string rngSequence;

    private Sprite[] spriteArray;
    private List<string> listCardNames;
    private int[] numericalDeck;
    private System.Random numberGenerator = new System.Random();
    private bool startUpdating = false;
    private bool found = false;

    private Dictionary<int, string> callsDict = new Dictionary<int, string>()
    {
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

    public void Initialize()
    {
        activeCards = new List<Card>();
        inactiveCards = new List<Card>();
        spriteArray = new Sprite[40];
        listCardNames = new List<string>();
        numericalDeck = new int[40];

        List<GameObject> sortObjects = new List<GameObject>();
        sortObjects.AddRange(GameObject.FindGameObjectsWithTag("Card"));
        sortObjects.Sort((a, b) => a.GetComponent<Card>().CompareTo(b));

        foreach (GameObject go in sortObjects)
            activeCards.Add(go.GetComponent<Card>());

        spriteArray = Resources.LoadAll<Sprite>("Textures/cards");

        foreach (Sprite s in spriteArray)
            Game.Instance.cardStringToSprite.Add(s.ToString().Split(' ')[0], s);

        if (PhotonNetwork.isMasterClient)
        {
            SetupDeck();
            photonView.RPC("RpcDealCards", PhotonTargets.All, rngSequence);
        }
    }

    void Update ()
    {
        
        if (!startUpdating)
            return;

        if (activeCards.Count % 2 == 0)
        {
            for (int i = 0; i <= activeCards.Count / 2 - 1; i++)
            {
                if (cardDragged == activeCards[i])
                    continue;

                RectTransform rt = activeCards[i].GetComponent<RectTransform>();
                rt.localPosition = Vector3.Lerp(rt.localPosition, new Vector3(activeCards[i].GetPositionObject().localPosition.x,
                    activeCards[i].GetPositionObject().localPosition.y - Mathf.Pow((Mathf.Abs(i - activeCards.Count / 2) + 1), 2) * 3), Time.deltaTime * 3);
            }

            for (int i = activeCards.Count / 2 + 1; i < activeCards.Count + 1; i++)
            {
                if (cardDragged == activeCards[i - 1])
                    continue;

                RectTransform rt = activeCards[i - 1].GetComponent<RectTransform>();
                rt.localPosition = Vector3.Lerp(rt.localPosition, new Vector3(activeCards[i - 1].GetPositionObject().localPosition.x,
                    activeCards[i - 1].GetPositionObject().localPosition.y - Mathf.Pow((Mathf.Abs(i - activeCards.Count / 2) + 1), 2) * 3), Time.deltaTime * 3);
            }
        }
        else
        {
            for (int i = 0; i <= activeCards.Count / 2; i++)
            {
                if (cardDragged == activeCards[i])
                    continue;

                RectTransform rt = activeCards[i].GetComponent<RectTransform>();
                rt.localPosition = Vector3.Lerp(rt.localPosition, new Vector3(activeCards[i].GetPositionObject().localPosition.x,
                    activeCards[i].GetPositionObject().localPosition.y - Mathf.Pow((Mathf.Abs(i - activeCards.Count / 2) + 1), 2) * 3), Time.deltaTime * 3);
            }

            for (int i = activeCards.Count / 2 + 1; i < activeCards.Count; i++)
            {
                if (cardDragged == activeCards[i])
                    continue;

                RectTransform rt = activeCards[i].GetComponent<RectTransform>();
                rt.localPosition = Vector3.Lerp(rt.localPosition, new Vector3(activeCards[i].GetPositionObject().localPosition.x,
                    activeCards[i].GetPositionObject().localPosition.y - Mathf.Pow((Mathf.Abs(i - activeCards.Count / 2) + 1), 2) * 3), Time.deltaTime * 3);
            }
        }

        for (int i = 0; i < activeCards.Count; i++)
        {
            if (cardDragged == activeCards[i])
                continue;

            RectTransform rt = activeCards[i].GetComponent<RectTransform>();
            rt.localEulerAngles = new Vector3(0, 0, (activeCards.Count - 1) - i * 2);
        }

        switch (activeCards.Count)
        {
            case 10:
                GetComponent<HorizontalLayoutGroup>().spacing = -280;
                break;
            case 9:
                GetComponent<HorizontalLayoutGroup>().spacing = -280 * 2;
                break;
            case 8:
                GetComponent<HorizontalLayoutGroup>().spacing = -280 * 3;
                break;
            case 6:
                GetComponent<HorizontalLayoutGroup>().spacing = -280 * 4;
                break;
            case 3:
                GetComponent<HorizontalLayoutGroup>().spacing = -280 * 5;
                break;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        inManager = true;
        found = false;

        if (isCardDragged)
        {
            activeCards.Insert(cardDragged.GetIndex(), cardDragged); //= AddDraggedCardAt(activeCards, cardDragged.GetIndex());

            for (int i = 0; i < activeCards.Count; i++)
            {
                if (activeCards[i].GetComponent<Card>().IsDragged())
                {
                    activeCards[i].GetPositionObject().gameObject.SetActive(true);
                    found = true;
                }
                else if (found)
                {
                    activeCards[i].IncrementIndex();
                }
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        inManager = false;
        found = false;

        if (isCardDragged)
        {
            for (int i = 0; i < activeCards.Count; i++)
            {
                if (activeCards[i].GetComponent<Card>().IsDragged() && !found)
                {
                    activeCards[i].GetPositionObject().gameObject.SetActive(false);
                    activeCards.RemoveAt(i);

                    if (i != activeCards.Count)
                        activeCards[i].DecrementIndex();

                    found = true;
                }
                else if (found)
                {
                    activeCards[i].DecrementIndex();
                }
            }
        }
    }

    private void SetupDeck()
    {
        for (int i = 0; i < 40; i++)
            numericalDeck[i] = i;

        Shuffle(numericalDeck);
        Shuffle(numericalDeck);

        foreach (int i in numericalDeck)
            rngSequence += i.ToString() + " ";
    }

    private void Shuffle(int[] deck)
    {
        for (int n = deck.Length - 1; n > 0; --n)
        {
            int k = numberGenerator.Next(n + 1);
            int temp = deck[n];
            deck[n] = deck[k];
            deck[k] = temp;
        }
    }

    [PunRPC]
    private void RpcDealCards(string rngSequence)
    {
        if (PhotonNetwork.playerName == Game.Instance.cardPlayers[0].playerSlotText.text)
            DealCards(rngSequence, 0);
        else if (PhotonNetwork.playerName == Game.Instance.cardPlayers[1].playerSlotText.text)
            DealCards(rngSequence, 10);
        else if (PhotonNetwork.playerName == Game.Instance.cardPlayers[2].playerSlotText.text)
            DealCards(rngSequence, 20);
        else if (PhotonNetwork.playerName == Game.Instance.cardPlayers[3].playerSlotText.text)
            DealCards(rngSequence, 30);
    }

    private void DealCards(string rngSequence, int gap)
    {
        List<Sprite> sortCardsInHand = new List<Sprite>();
        SpriteRenderer tempSpriteRenderer;

        for (int i = 0; i < 10; i++)
            sortCardsInHand.Add(spriteArray[System.Int32.Parse(rngSequence.Split(' ')[i + gap])]);

        sortCardsInHand = sortCardsInHand.OrderBy(p => p.ToString()[3]).ToList();

        for (int i = 0; i < 10; i++)
        {
            tempSpriteRenderer = activeCards[i].GetComponent<SpriteRenderer>();
            tempSpriteRenderer.sprite = sortCardsInHand[i];
            tempSpriteRenderer.name = sortCardsInHand[i].name;

            activeCards[i].GetComponent<BoxCollider2D>().enabled = false;
            activeCards[i].GetPositionObject().gameObject.SetActive(true);
        }

        foreach (Sprite s in sortCardsInHand)
            listCardNames.Add(s.ToString().Split(' ')[0]);

        startUpdating = true;
    }

    public void Call(Dropdown target)
    {
        if (target.value == 1)
        {
            if (listCardNames.Contains("103k") && listCardNames.Contains("103b") && listCardNames.Contains("103d") && listCardNames.Contains("103s"))
            {
                photonView.RPC("RpcCall", PhotonTargets.All, 12, 0, PhotonNetwork.playerName);
            }
            else if (listCardNames.Contains("103k") && listCardNames.Contains("103b") && listCardNames.Contains("103d"))
            {
                photonView.RPC("RpcCall", PhotonTargets.All, 9, 1, PhotonNetwork.playerName);
            }
            else if (listCardNames.Contains("103k") && listCardNames.Contains("103b") && listCardNames.Contains("103s"))
            {
                photonView.RPC("RpcCall", PhotonTargets.All, 9, 2, PhotonNetwork.playerName);
            }
            else if (listCardNames.Contains("103s") && listCardNames.Contains("103b") && listCardNames.Contains("103d"))
            {
                photonView.RPC("RpcCall", PhotonTargets.All, 9, 3, PhotonNetwork.playerName);
            }
            else if (listCardNames.Contains("103d") && listCardNames.Contains("103s") && listCardNames.Contains("103k"))
            {
                photonView.RPC("RpcCall", PhotonTargets.All, 9, 4, PhotonNetwork.playerName);
            }
        }
        else if (target.value == 2)
        {
            if (listCardNames.Contains("102k") && listCardNames.Contains("102b") && listCardNames.Contains("102d") && listCardNames.Contains("102s"))
            {
                photonView.RPC("RpcCall", PhotonTargets.All, 12, 5, PhotonNetwork.playerName);
            }
            else if (listCardNames.Contains("102k") && listCardNames.Contains("102b") && listCardNames.Contains("102d"))
            {
                photonView.RPC("RpcCall", PhotonTargets.All, 9, 6, PhotonNetwork.playerName);
            }
            else if (listCardNames.Contains("102k") && listCardNames.Contains("102b") && listCardNames.Contains("102s"))
            {
                photonView.RPC("RpcCall", PhotonTargets.All, 9, 7, PhotonNetwork.playerName);
            }
            else if (listCardNames.Contains("102s") && listCardNames.Contains("102b") && listCardNames.Contains("102d"))
            {
                photonView.RPC("RpcCall", PhotonTargets.All, 9, 8, PhotonNetwork.playerName);
            }
            else if (listCardNames.Contains("102d") && listCardNames.Contains("102s") && listCardNames.Contains("102k"))
            {
                photonView.RPC("RpcCall", PhotonTargets.All, 9, 9, PhotonNetwork.playerName);
            }
        }
        else if (target.value == 3)
        {
            if (listCardNames.Contains("101k") && listCardNames.Contains("101b") && listCardNames.Contains("101d") && listCardNames.Contains("101s"))
            {
                photonView.RPC("RpcCall", PhotonTargets.All, 12, 10, PhotonNetwork.playerName);
            }
            else if (listCardNames.Contains("101k") && listCardNames.Contains("101b") && listCardNames.Contains("101d"))
            {
                photonView.RPC("RpcCall", PhotonTargets.All, 9, 11, PhotonNetwork.playerName);
            }
            else if (listCardNames.Contains("101k") && listCardNames.Contains("101b") && listCardNames.Contains("101s"))
            {
                photonView.RPC("RpcCall", PhotonTargets.All, 9, 12, PhotonNetwork.playerName);
            }
            else if (listCardNames.Contains("101s") && listCardNames.Contains("101b") && listCardNames.Contains("101d"))
            {
                photonView.RPC("RpcCall", PhotonTargets.All, 9, 13, PhotonNetwork.playerName);
            }
            else if (listCardNames.Contains("101d") && listCardNames.Contains("101s") && listCardNames.Contains("101k"))
            {
                photonView.RPC("RpcCall", PhotonTargets.All, 9, 14, PhotonNetwork.playerName);
            }
        }
        else if (target.value == 4)
        {
            if (listCardNames.Contains("101k") && listCardNames.Contains("102k") && listCardNames.Contains("103k"))
            {
                photonView.RPC("RpcCall", PhotonTargets.All, 9, 15, PhotonNetwork.playerName);
            }
            else if (listCardNames.Contains("101d") && listCardNames.Contains("102d") && listCardNames.Contains("103d"))
            {
                photonView.RPC("RpcCall", PhotonTargets.All, 9, 16, PhotonNetwork.playerName);
            }
            else if (listCardNames.Contains("101s") && listCardNames.Contains("102s") && listCardNames.Contains("103s"))
            {
                photonView.RPC("RpcCall", PhotonTargets.All, 9, 17, PhotonNetwork.playerName);
            }
            else if (listCardNames.Contains("101b") && listCardNames.Contains("102b") && listCardNames.Contains("103b"))
            {
                photonView.RPC("RpcCall", PhotonTargets.All, 9, 18, PhotonNetwork.playerName);
            }
        }
    }

    [PunRPC]
    void RpcCall(int points, int loc, string src)
    {
        if (src == Game.Instance.cardPlayers[0].playerSlotText.text || src == Game.Instance.cardPlayers[2].playerSlotText.text)
        {
            Game.Instance.roundScore1 += points;
        }
        else if (src == Game.Instance.cardPlayers[1].playerSlotText.text || src == Game.Instance.cardPlayers[3].playerSlotText.text)
        {
            Game.Instance.roundScore2 += points;
        }

        Game.Instance.textHF.text = src + callsDict[loc];
    }

    public void EnableCardInteraction()
    {
        foreach (Card c in activeCards)
            c.GetComponent<BoxCollider2D>().enabled = true;
    }

    public void DisableCardInteraction()
    {
        foreach (Card c in activeCards)
            c.GetComponent<BoxCollider2D>().enabled = false;
    }

    public void ClearCards()
    {
        foreach (Card c in inactiveCards)
            c.gameObject.SetActive(true);

        foreach (GameObject go in GameObject.FindGameObjectsWithTag("Card"))
            go.GetComponent<Card>().ResetName();
    }
}
