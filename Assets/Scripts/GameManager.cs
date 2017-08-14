using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;

public class GameManager : NetworkManager {

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
		
	public void CmdRespawn (GameObject player) {
		Respawn (player);
	}

}