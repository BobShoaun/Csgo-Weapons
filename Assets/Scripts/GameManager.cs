using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;
using UnityEngine.Networking.NetworkSystem;

public class GameManager : NetworkManager {

	// Server
	private Dictionary<int, Player> players = new Dictionary<int, Player> ();

	// Both
	private const short chatMessageType = 131;
	private const short killFeedMessageType = 132;
	private const short scoreboardMessageType = 133;

	public static GameManager Instance {
		get { return singleton as GameManager; }
	}

	// Client
	private Player localPlayer;
	public Player LocalPlayer {
		set { 
			localPlayer = value;
			//PlayerHUD.Instance.SetLocalPlayer (value.connectionToClient.connectionId);
		}
	}

	public override void OnServerConnect (NetworkConnection conn) {
		base.OnServerConnect (conn);
	}

	public override void OnServerAddPlayer (NetworkConnection conn, short playerControllerId) {
		base.OnServerAddPlayer (conn, playerControllerId);
		Player player = conn.playerControllers [0].gameObject.GetComponent<Player> ();
		player.name = "Player " + conn.connectionId;

		NetworkServer.SendToAll (chatMessageType, 
			new StringMessage ("[Server]: " + player.name + " has joined the game"));
		// UPdate scoreboard for new player
		foreach (var existingPlayer in players.Values) {
			NetworkServer.SendToClient (conn.connectionId, scoreboardMessageType, 
				new ScoreboardMessage (existingPlayer.connectionToClient.connectionId, existingPlayer.name, 
					existingPlayer.kills, existingPlayer.assists, existingPlayer.deaths));
		}
		// Update for existing players
		NetworkServer.SendToAll (scoreboardMessageType, 
			new ScoreboardMessage (conn.connectionId, player.name, player.kills, player.assists, player.deaths));

		players.Add (conn.connectionId, player);
	}

	public override void OnClientConnect (NetworkConnection conn) {
		base.OnClientConnect (conn);
	}

	public override void OnStartServer () {
		base.OnStartServer ();
		NetworkServer.RegisterHandler (chatMessageType, ServerRelayChat);
	}

	public override void OnStopServer () {
		base.OnStopServer ();
		NetworkServer.UnregisterHandler (chatMessageType);
	}

	public override void OnStartClient (NetworkClient client) {
		base.OnStartClient (client);
		client.RegisterHandler (chatMessageType, ReceiveChat);
		client.RegisterHandler (killFeedMessageType, ReceiveKillFeed);
		client.RegisterHandler (scoreboardMessageType, ReceiveScoreboardUpdate);
	}

	public override void OnStopClient () {
		base.OnStopClient ();
		client.UnregisterHandler (chatMessageType);
		client.UnregisterHandler (killFeedMessageType);
		client.UnregisterHandler (scoreboardMessageType);
	}

	public void SendChat (string message) {
		client.Send (chatMessageType, new StringMessage (message));
	}

	private void ReceiveChat (NetworkMessage networkMessage) {
		PlayerHUD.Instance.ReceiveChat (networkMessage.ReadMessage<StringMessage> ().value);
	}

	private void ServerRelayChat (NetworkMessage networkMessage) {
		Player player;
		if (players.TryGetValue (networkMessage.conn.connectionId, out player)) {
			NetworkServer.SendToAll (chatMessageType, 
				new StringMessage (player.name + ": " + 
					networkMessage.ReadMessage<StringMessage> ().value));
		}
	}

	public void Respawn (GameObject player) {

		GameObject newPlayer = Instantiate (playerPrefab);

		NetworkIdentity n = player.GetComponent<NetworkIdentity> ();

		NetworkServer.ReplacePlayerForConnection (
			n.connectionToClient, 
			newPlayer,
			n.playerControllerId);
		Destroy (player);
		NetworkServer.Spawn (newPlayer);
	}
		
	public void RespawnPlayer () {
		localPlayer.CmdRespawn ();
		//Respawn (player);
		//client.Send (
	}

	public void PlayerDie (GameObject player, GameObject murderer, bool headShot, bool wallBang) {
		player.GetComponent<Player> ().deaths++;
		murderer.GetComponent<Player> ().kills++;
	}

	public void SendKillFeed (int killedConnectionId, int killerConnectionId, int assistConnectionId, int weaponId, bool headShot, bool wallBang) {
		Player killed, killer, assistKiller;
		if (players.TryGetValue (killedConnectionId, out killed) && 
			players.TryGetValue (killerConnectionId, out killer) &&
			players.TryGetValue (assistConnectionId, out assistKiller)) {
			NetworkServer.SendToAll (killFeedMessageType, new KillFeedMessage (killed.name, 
				killer.name, assistKiller.name, weaponId, headShot, wallBang));
		}
	}

	public void SendKillFeed (int killedConnectionId, int killerConnectionId, int weaponId, bool headShot, bool wallBang) {
		Player killed, killer;
		if (players.TryGetValue (killedConnectionId, out killed) && 
			players.TryGetValue (killerConnectionId, out killer)) {
			NetworkServer.SendToAll (killFeedMessageType, new KillFeedMessage (killed.name, 
				killer.name, string.Empty, weaponId, headShot, wallBang));
		}
	}

	private void ReceiveKillFeed (NetworkMessage networkMessage) {
		var killFeedMsg = networkMessage.ReadMessage<KillFeedMessage> ();
		PlayerHUD.Instance.UpdateKillFeedList (killFeedMsg.killed, killFeedMsg.killer, killFeedMsg.assistKiller,
			killFeedMsg.weaponId, killFeedMsg.headShot, killFeedMsg.wallBang);
	}

	private void ReceiveScoreboardUpdate (NetworkMessage networkMessage) {
		var msg = networkMessage.ReadMessage<ScoreboardMessage> ();
		PlayerHUD.Instance.AddPlayerToScoreboard (msg.connectionId, msg.name, 
			msg.kills, msg.assists, msg.deaths);
	}

	private class KillFeedMessage : MessageBase {

		public string killed;
		public string killer;
		public string assistKiller;
		public int weaponId;
		public bool headShot;
		public bool wallBang;

		public KillFeedMessage () { }

		public KillFeedMessage (string killed, string killer, string assistKiller, int weaponId, bool headShot, bool wallBang) {
			this.killed = killed;
			this.killer = killer;
			this.assistKiller = assistKiller;
			this.weaponId = weaponId;
			this.headShot = headShot;
			this.wallBang = wallBang;
		}

	}

	private class ScoreboardMessage : MessageBase {

		public int connectionId;
		public string name;
		public int kills;
		public int assists;
		public int deaths;

		public ScoreboardMessage () {
			
		}

		public ScoreboardMessage (int connectionId, string name, int kills, int assists, int deaths) {
			this.connectionId = connectionId;
			this.name = name;
			this.kills = kills;
			this.assists = assists;
			this.deaths = deaths;
		}

	}

	public class BooleanMessage : MessageBase {

		public bool value;

		public BooleanMessage (bool value) {
			this.value = value;
		}

	}

}