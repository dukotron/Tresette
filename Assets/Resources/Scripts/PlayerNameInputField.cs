using UnityEngine;
using System.Collections;
using UnityEngine.UI;

[RequireComponent(typeof(InputField))]
public class PlayerNameInputField : MonoBehaviour {

	static string playerNamePrefKey = "PlayerName";

	void Start () {
		string defaultName = "";
		InputField _inputField = this.GetComponent<InputField> ();
		if (_inputField != null) {
			if (PlayerPrefs.HasKey (playerNamePrefKey)) {
				defaultName = PlayerPrefs.GetString (playerNamePrefKey);
				_inputField.text = defaultName;
			}
		}

		PhotonNetwork.playerName = defaultName + "#" + System.DateTime.Now.Minute + System.DateTime.Now.Second + Random.Range(1, 999);
	}

	public void SetPlayerName (string value) {
        string temp = value + "#" + System.DateTime.Now.Minute + System.DateTime.Now.Second + Random.Range(1, 999);
        PhotonNetwork.playerName = temp;
		PlayerPrefs.SetString (playerNamePrefKey, temp.Split('#')[0]);
	} 
}
