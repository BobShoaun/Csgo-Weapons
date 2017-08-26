using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class TestNetworking : NetworkBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

		if (Input.GetKeyDown (KeyCode.L))
			CmdDoSmth ();
	}

	[Command]
	private void CmdDoSmth () {
		Debug.Log ("smth");
	}
}
