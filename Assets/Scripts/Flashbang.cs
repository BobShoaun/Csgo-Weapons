﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Flashbang : DroppedWeapon, IGrenade {

	public float timer = 2;
	public Collider sphere;
	public Collider capsule;
	private Player playerPrimer;
	public LayerMask mask;

	protected override void Start () {
		base.Start ();
		sphere = GetComponent<SphereCollider> ();
		capsule = GetComponent<CapsuleCollider> ();
	}

	public void Prime (Player primer) {
		playerPrimer = primer;
		capsule.enabled = false;
		sphere.enabled = true;
		StartCoroutine (Timer ());
	}

	private IEnumerator Timer () {
		yield return new WaitForSeconds (timer * 3f / 4f);
		capsule.enabled = true;
		sphere.enabled = false;
		yield return new WaitForSeconds (timer / 4f);
		CmdExplode ();
	}

	[Command]
	public void CmdExplode () {
		var flashedPlayers = new List<GameObject> ();
		var isDirectFlash = new List<bool> ();
		// HACK: Find better way to get a list of all players in the server
		foreach (var player in FindObjectsOfType<Player> ()) {
			if (!Physics.Linecast (transform.position, player.transform.position, mask)) {
				var screenPoint = player.GetComponentInChildren<Camera> ().WorldToViewportPoint (transform.position);
				var onScreen = screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0
				               && screenPoint.y < 1;
				isDirectFlash.Add (onScreen);
				flashedPlayers.Add (player.gameObject);
			}
		}
		RpcFlash (flashedPlayers.ToArray (), isDirectFlash.ToArray ());
		Destroy (gameObject);

	}

	[ClientRpc]
	private void RpcFlash (GameObject [] flashedPlayers, bool [] direct) {

		// TODO flash sound, particles and light
		for (var i = 0; i < flashedPlayers.Length; i++) {
			//flashedPlayers [i].GetComponent<Player> ().Flashed (direct [i]);
			if (flashedPlayers [i].GetComponent<NetworkIdentity> ().isLocalPlayer)
				PlayerHUD.Instance.Flash (direct [i]);
		}

		// DONE : shoudnt be destryed here

	}


}