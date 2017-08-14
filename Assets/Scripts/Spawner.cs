using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Spawner : NetworkBehaviour {

	public GameObject crate;

	[Server]
	public override void OnStartServer () {
		base.OnStartServer ();
		//NetworkServer.Spawn (c);
		NetworkServer.SpawnObjects ();
	}

	void Update () {
		if (Input.GetKeyDown (KeyCode.R)) {
			var c = Instantiate (crate);
			NetworkServer.Spawn (c);
			//NetworkServer.SpawnWithClientAuthority (c, GameObject.Find ("Player 4"));
		}	
	}

	public void OnServerInitialized () {
		print ("server start");
	}
//
//
//	// Use this for initialization
//	void Start () {
//		if (!isServer)
//			return;
//		var c = Instantiate (crate);
//		NetworkServer.Spawn (c);
//	}
//	
//	// Update is called once per frame
//	void Update () {
//		
//	}
}
