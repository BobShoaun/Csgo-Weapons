using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using Doxel.Utility;
using DUtil = Doxel.Utility.Utility;
using System.Text;

public class PlayerHUD : SingletonMonoBehaviour<PlayerHUD> {

	public GameObject shopMenu;
	public GameObject gameOverPanel;
	public GameObject healthPanel;
	public Transform killFeedList;
	public Text killFeedPrefab;
	public GameObject chatMessageInput;
	public GameObject chatMessagePrefab;
	public GameObject scorePrefab;
	public GameObject scoreboard;
	public Transform scoreList;
	private Text healthText;
	private Button send;
	private InputField chatMessageInputField;
	public Transform chatMessageList;
	public Image flashOverlay;
	public RawImage burntImage;
	public RenderTexture radar;

	public Text weaponName;
	public Text weaponAmmo;
	public Text weaponReserve;
	public Text hoverPickup;

	public GameObject scopeOverlay;
	public GameObject crossHair;
	public GameObject damageIndicator;
	public WeaponDatabase weaponDatabase;

	public string WeaponName {
		set {
			weaponName.text = value;
		}
	}
	public int WeaponAmmo {
		set {
			weaponAmmo.text = value + " /";
		}
	}	
	public int WeaponReserve {
		set {
			weaponReserve.text = value.ToString ();
		}
	}


	bool chatOpen;

	void OnEnable () {
		//NetworkManager.singleton.
	}

	void OnDisable () {
		//player.OnHealthChanged -= health => healthText.text = health.ToString ();
	} 

	void Start () {
		chatMessageInputField = chatMessageInput.GetComponentInChildren<InputField> ();
		healthText = healthPanel.GetComponentInChildren<Text> ();
	}

	void Update () {
		if (Input.GetKeyDown (KeyCode.T)) {
			chatMessageInput.SetActive (true);
			chatMessageInputField.ActivateInputField ();
			chatOpen = true;
			//player.GetComponent<PlayerController> ().enabled = false;
			//msgInput.
		}
		if (Input.GetKeyDown (KeyCode.Escape)) {
			CloseChat ();
		}
		if (chatOpen && Input.GetKeyDown (KeyCode.Return)) {
			SendChat (chatMessageInputField.text);
		} 
		if (Input.GetKeyDown (KeyCode.B) && !shopMenu.activeSelf)
			shopMenu.SetActive (true);
		else if (Input.GetKeyDown (KeyCode.B) || Input.GetKeyDown (KeyCode.Escape) && shopMenu.activeSelf)
			shopMenu.SetActive (false);
		scoreboard.SetActive (Input.GetKey (KeyCode.Tab));
	}

	private void CloseChat () {
		chatMessageInput.SetActive (false);
		chatMessageInputField.text = string.Empty;
		chatOpen = false;
		//player.GetComponent<PlayerController> ().enabled = true;
	}

	public void SendChat (string msg) {
		CloseChat ();
		GameManager.Instance.SendChat (msg);
	}

	public void ReceiveChat (string msg) {
		GameObject chatObj = Instantiate (chatMessagePrefab, chatMessageList);
		chatObj.GetComponent<Text> ().text = msg;
		Destroy (chatObj, 20);
	}

	public void DisplayDeathUI (GameObject murderer) {
		gameOverPanel.SetActive (true);
		healthPanel.SetActive (false);
	}

	public void UpdateKillFeedList (string killed, string killer, string assist, int weaponId, bool headShot, bool wallBang) {
		var killFeed = Instantiate (killFeedPrefab, killFeedList);
		var killFeedText = new StringBuilder (killer);
		if (assist != string.Empty)
			killFeedText.Append (" + ").Append (assist);
		killFeedText.Append (' ').Append (weaponDatabase [weaponId].Name);
		if (wallBang)
			killFeedText.Append (' ').Append ("WallBang");
		if (headShot)
			killFeedText.Append (' ').Append ("HeadShot");
		killFeedText.Append (' ').Append (killed);
		killFeed.text = killFeedText.ToString ();
		Destroy (killFeed.gameObject, 10);
	}
		
	public void PlayerRespawn () {
		gameOverPanel.SetActive (false);
		healthPanel.SetActive (true);

		//(GameManager.singleton as GameManager).CmdRespawn (player.gameObject);
		//player.CmdRespawn ();
		GameManager.Instance.RespawnPlayer ();
	}

	public void UpdateHealth (int health) {
		healthText.text = health.ToString () + " / 100";
	}

	public void Flash (bool direct) {
		StartCoroutine (FlashSequence (direct));
	}

	private IEnumerator FlashSequence (bool direct) {

		yield return new WaitForEndOfFrame ();
		burntImage.texture = ScreenShot ();
		burntImage.enabled = true;
		flashOverlay.enabled = true;
		if (direct) {
			burntImage.color = Color.white;
			flashOverlay.color = Color.white;
			yield return new WaitForSeconds (2);
			StartCoroutine (DUtil.Fade (result => flashOverlay.color = result, 2, flashOverlay.color, Color.clear));
			yield return DUtil.Fade (result => burntImage.color = result, 3, burntImage.color, Color.clear);
		} 
		else {
			flashOverlay.color = DUtil.translucent;
			burntImage.color = DUtil.translucent;
			yield return new WaitForSeconds (1);
			StartCoroutine (DUtil.Fade (result => flashOverlay.color = result, 1, flashOverlay.color, Color.clear));
			yield return DUtil.Fade (result => burntImage.color = result, 1, burntImage.color, Color.clear);
		}
		flashOverlay.enabled = false;
		burntImage.enabled = false;
	}

	private Texture ScreenShot () {
		var result = new Texture2D (Screen.width, Screen.height, TextureFormat.RGB24, false);
		result.ReadPixels (new Rect (0, 0, Screen.width, Screen.height), 0, 0);
		result.Apply ();
		return result;
	}

	public void HoverPickup (string weaponName) {
		hoverPickup.text = "Press [E] to pickup " + weaponName;
		hoverPickup.enabled = true;
	}

	public void HoverDeactivate () {
		hoverPickup.enabled = false;
		hoverPickup.text = string.Empty;
	}

	private Dictionary<int, ScoreUI> scoreUIs = new Dictionary<int, ScoreUI> ();

	public void AddPlayerToScoreboard (int connectionId, string name, int kills, int assists, int deaths) {
		ScoreUI score = Instantiate (scorePrefab, scoreList).GetComponent<ScoreUI> ();
		scoreUIs.Add (connectionId, score);
		score.Name = name;
		score.Kills = kills;
		score.Assists = assists;
		score.Deaths = deaths;
	}

	public void SetLocalPlayer (int connectionId) {
		ScoreUI score;
		if (scoreUIs.TryGetValue (connectionId, out score))
			score.IsLocalPlayer = true;
	}

	public void UpdateKills (int connectionId, int kills) {
		ScoreUI score;
		if (scoreUIs.TryGetValue (connectionId, out score))
			score.Kills = kills;
	}

	public void UpdateDeaths (int connectionId, int deaths) {
		ScoreUI score;
		if (scoreUIs.TryGetValue (connectionId, out score))
			score.Deaths = deaths;
	}

	public void UpdateAssists (int connectionId, int assists) {
		ScoreUI score;
		if (scoreUIs.TryGetValue (connectionId, out score))
			score.Assists = assists;
	}

	public void SetRadarCam (Camera radarCam) {
		radarCam.targetTexture = radar;
	}

	public void DisplayDamageIndicator (Vector3 forward, Vector3 direction) {
		damageIndicator.SetActive (true);
		StartCoroutine (DUtil.DelayedInvoke (() => damageIndicator.SetActive (false), 2));
		damageIndicator.transform.rotation = 
			Quaternion.Euler (Vector3.forward *
				-Vector3.SignedAngle (transform.forward, direction, Vector3.up));
	}
		
}