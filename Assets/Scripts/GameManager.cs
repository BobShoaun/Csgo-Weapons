using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;
using UnityEngine.Networking.NetworkSystem;

public class GameManager : NetworkManager {

	private const short chatMessageType = 131;

	public static GameManager Instance {
		get { 
			return singleton as GameManager;
		}
	}

	public Player localPlayer;

	private void Start () {
		foreach (Player player in FindObjectsOfType<Player> ()) {
			if (player.isLocalPlayer)
				localPlayer = player;
		}
		//if (NetworkClient.active) {
		client.RegisterHandler (chatMessageType, ReceiveChat);
		//}
	
		if (NetworkServer.active) {
			NetworkServer.RegisterHandler (chatMessageType, ServerReceiveChat);
		}
	}

	public void SendChat (string message) {
		client.Send (chatMessageType, new StringMessage (message));
		//RpcReceiveChat (localPlayer.name + ": " + message);
		//client.Send (message, 
	}

	private void ReceiveChat (NetworkMessage networkMessage) {
		PlayerHUD.Instance.ReceiveChat (networkMessage.ReadMessage<StringMessage> ().value);
	}

	private void ServerReceiveChat (NetworkMessage networkMessage) {
		NetworkServer.SendToAll (chatMessageType, 
			new StringMessage (networkMessage.conn.connectionId + ": " + 
				networkMessage.ReadMessage<StringMessage> ().value));
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



}