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

	public Player localPlayer;

	public override void OnServerConnect (NetworkConnection conn) {
		base.OnServerConnect (conn);

	}

	public override void OnServerAddPlayer (NetworkConnection conn, short playerControllerId) {
		base.OnServerAddPlayer (conn, playerControllerId);
		Player player = conn.playerControllers [0].gameObject.GetComponent<Player> ();
		player.name = "Player " + conn.connectionId;
		players.Add (conn.connectionId, 
			player);
		NetworkServer.SendToAll (chatMessageType, 
			new StringMessage ("[Server]: " + player.name + " has joined the game"));
		
	}

	public override void OnStartServer () {
		base.OnStartServer ();
		NetworkServer.RegisterHandler (chatMessageType, ServerTransportChat);
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

	private void ServerTransportChat (NetworkMessage networkMessage) {
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
		
	public void CmdRespawn () {
		localPlayer.CmdRespawn ();
		//Respawn (player);
		//client.Send (
	}

	public void PlayerDie (GameObject player, GameObject murderer, bool headShot, bool wallBang) {
		player.GetComponent<Player> ().deaths++;
		murderer.GetComponent<Player> ().kills++;
	}

	public void SendKillFeed (int connectionId, int killerConnectionId, bool headShot, bool wallBang) {
		Player player, killer;
		if (players.TryGetValue (connectionId, out player) && 
			players.TryGetValue (killerConnectionId, out killer)) {
			string killFeed = killer.name;
			if (wallBang)
				killFeed += " Walled";
			killFeed += (headShot ? " HeadShot " : " Killed ") + player.name;
			NetworkServer.SendToAll (killFeedMessageType, new StringMessage (killFeed));
		}
	}

	private void ReceiveKillFeed (NetworkMessage networkMessage) {
		PlayerHUD.Instance.UpdateKillFeedList (networkMessage.ReadMessage<StringMessage> ().value);
	}

	private void ReceiveScoreboardUpdate (NetworkMessage networkMessage) {
		var msg = networkMessage.ReadMessage<ScoreboardMessage> ();
		PlayerHUD.Instance.AddPlayerToScoreboard (msg.connectionId, msg.name, msg.isLocalPlayer);
	}

	public void ServerUpdateScoreboard (int connectionId) {
		Player player;
		if (players.TryGetValue (connectionId, out player)) {
			NetworkServer.SendToClient (connectionId, scoreboardMessageType, 
					new ScoreboardMessage (connectionId, player.name, player.isLocalPlayer));
		}
	}

	private class ScoreboardMessage : MessageBase {

		public int connectionId;
		public string name;
		public bool isLocalPlayer;

		public ScoreboardMessage () {
			
		}

		public ScoreboardMessage (int connectionId, string name, bool isLocalPlayer) {
			this.connectionId = connectionId;
			this.name = name;
			this.isLocalPlayer = isLocalPlayer;
		}

	}

	public class BooleanMessage : MessageBase {

		public bool value;

		public BooleanMessage (bool value) {
			this.value = value;
		}

	}

}