using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using Doxel.Utility;
using DUtil = Doxel.Utility.Utility;

public class PlayerHUD : SingletonMonoBehaviour<PlayerHUD> {
	
	public GameObject gameOverPanel;
	public GameObject healthPanel;
	public Transform killFeedList;
	public GameObject killFeedPrefab;
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

	private Player player;
	public Player Player {
		get { return player; }
		set {
			player = value;
			player.OnHealthChanged += DisplayHealth;
			player.Die += PlayerDied;
			//player.Flash += direct => StartCoroutine (FlashSequence (direct));
			//Player.DeathNote = UpdateKillFeedList;
			Player.SendChatMessage = ClientReceiveChat;
			healthText = healthPanel.GetComponentInChildren<Text> ();
			DisplayHealth (player.health);
		}
	}

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
		player.CmdSendChat (player.name + ": " + msg);
	}

	private void ClientReceiveChat (string msg) {
		GameObject chatObj = Instantiate (chatMessagePrefab, chatMessageList);
		chatObj.GetComponent<Text> ().text = msg;
		Destroy (chatObj, 20);
	}

	void PlayerDied (GameObject murderer) {
		gameOverPanel.SetActive (true);
		healthPanel.SetActive (false);
	}

	public void UpdateKillFeedList (string note) {
		GameObject killFeed = Instantiate (killFeedPrefab, killFeedList);
		killFeed.GetComponent<Text> ().text = note;
		Destroy (killFeed, 10);
	}
		
	public void PlayerRespawn () {
		gameOverPanel.SetActive (false);
		healthPanel.SetActive (true);

		//(GameManager.singleton as GameManager).CmdRespawn (player.gameObject);
		player.CmdRespawn ();
	}

	void DisplayHealth (int health) {
		healthText.text = health.ToString () + " / 100";
	}

	public void Flash (bool direct) {
		StartCoroutine (FlashSequence (direct));
	}

	private IEnumerator FlashSequence (bool direct) {

		yield return new WaitForEndOfFrame ();
		burntImage.texture = Capture ();
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

	private Texture Capture () {
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

	private Dictionary<NetworkInstanceId, ScoreUI> scoreUIs = new Dictionary<NetworkInstanceId, ScoreUI> ();

	public void AddPlayerToScoreboard (NetworkInstanceId netId, string name, int kills, int deaths) {
		ScoreUI score = Instantiate (scorePrefab, scoreList).GetComponent<ScoreUI> ();
		scoreUIs.Add (netId, score);
		score.UpdateUI (name, kills, deaths);
	}

	public void UpdatePlayerScoreUI (NetworkInstanceId netId, string name, int kills, int deaths) {
		ScoreUI score;
		if (scoreUIs.TryGetValue (netId, out score)) {
			score.UpdateUI (name, kills, deaths);
		}
	}

	public void SetRadarCam (Camera radarCam) {
		radarCam.targetTexture = radar;
	}
		
}