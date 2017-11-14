using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card : Photon.PunBehaviour
{
    public RectTransform positionObject;

    private int index;
    private int finalIndex;

    private Color originalColor = Color.white;
    private Color mouseOverColor = Color.blue;
    private bool dragging = false;
    private float distance;

    float start;

    void OnMouseEnter()
    {
        //GetComponentInChildren<Renderer> ().material = mouseOverMaterial;,
        GetComponentInChildren<Renderer>().material.color = mouseOverColor;
    }

    void OnMouseExit()
    {
        GetComponentInChildren<Renderer>().material.color = originalColor;
    }

    void OnMouseDown()
    {
        distance = Vector3.Distance(transform.position, Camera.main.transform.position);
        dragging = true;
        GetComponent<RectTransform>().eulerAngles = Vector3.zero;
        CardManager.isCardDragged = true;
        CardManager.cardDragged = this;
    }

    void OnMouseUp()
    {
        dragging = false;
        CardManager.isCardDragged = false;
        CardManager.cardDragged = null;
        if (!CardManager.inManager)
        {
            gameObject.SetActive(false);

            foreach (Card c in CardManager.activeCards)
                c.GetComponent<BoxCollider2D>().enabled = false;

            Game.Instance.buttonFlip.interactable = false;
            Game.Instance.buttonHit.interactable = false;
            Game.Instance.dropDownCalls.interactable = false;

            CardManager.inactiveCards.Add(this);

            photonView.RPC("RpcThrowCard", PhotonTargets.All, PhotonNetwork.playerName, name);

            /*if (Game.Instance.turnCounter == 4)
                Game.Instance.FinishTurn();*/
        }
        else
            gameObject.SetActive(true);
    }

    void Start()
    {
        index = int.Parse(gameObject.name.Split(' ')[1].ToString());
        finalIndex = int.Parse(gameObject.name.Split(' ')[1].ToString());
    }

    void Update()
    {
        if (dragging)
        { 
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Vector3 rayPoint = ray.GetPoint(distance);
            transform.position = Vector3.Lerp(transform.position, rayPoint, Time.time);
        }
    }

    public bool IsDragged()
    {
        return dragging;
    }

    public RectTransform GetPositionObject()
    {
        return positionObject;
    }

    public int GetIndex()
    {
        return index;
    }

    public void IncrementIndex()
    {
        index += 1;
    }

    public void DecrementIndex()
    {
        index -= 1;
    }

    [PunRPC]
    private void RpcThrowCard(string player, string card)
    {
        Game.Instance.turnCounter += 1;

        if (player == Game.Instance.cardPlayers[0].playerSlotText.text)
        {
            Game.Instance.cardPlayers[0].playerSlotImage.sprite = Game.Instance.cardStringToSprite[card];

            if (Game.Instance.turnCounter != 5 && PhotonNetwork.playerName == Game.Instance.cardPlayers[1].playerSlotText.text)
                Game.Instance.StartTurn(false);
        }
        else if (player == Game.Instance.cardPlayers[1].playerSlotText.text)
        {
            Game.Instance.cardPlayers[1].playerSlotImage.sprite = Game.Instance.cardStringToSprite[card];

            if (Game.Instance.turnCounter != 5 && PhotonNetwork.playerName == Game.Instance.cardPlayers[2].playerSlotText.text)
                Game.Instance.StartTurn(false);
        }
        else if (player == Game.Instance.cardPlayers[2].playerSlotText.text)
        {
            Game.Instance.cardPlayers[2].playerSlotImage.sprite = Game.Instance.cardStringToSprite[card];

            if (Game.Instance.turnCounter != 5 && PhotonNetwork.playerName == Game.Instance.cardPlayers[3].playerSlotText.text)
                Game.Instance.StartTurn(false);
        }
        else
        {
            Game.Instance.cardPlayers[3].playerSlotImage.sprite = Game.Instance.cardStringToSprite[card];

            if (Game.Instance.turnCounter != 5 && PhotonNetwork.playerName == Game.Instance.cardPlayers[0].playerSlotText.text)
                Game.Instance.StartTurn(false);
        }

        if (Game.Instance.turnCounter == 1)
            Game.Instance.letterOfTurn = card[3].ToString();

        if (Game.Instance.turnCounter == 4)
            Game.Instance.FinishTurn();
    }

    public override string ToString()
    {
        return index.ToString();
    }

    public int CompareTo(object obj)
    {
        GameObject otherObject = (GameObject) obj;

        return name.CompareTo(otherObject.name);
    }

    public void ResetName()
    {
        name = "Card " + finalIndex;
    }
}