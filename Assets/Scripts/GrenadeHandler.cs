using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

public class GrenadeHandler : Handler {

	// Client


	// Server
	public Transform look;
	private Grenade grenade;


	protected override bool SetWeapon (Weapon weapon) {
		grenade = weapon as Grenade;
		return base.SetWeapon (grenade);
	}

	protected override void ServerKeep () {
		
	}

	protected override void ClientDeploy (GameObject firstPerson, GameObject thirdPerson) {
		
	}

	protected override void ClientUpdate () {
		if (!isLocalPlayer)
			return;

		if (Input.GetMouseButtonUp (0))
			CmdThrow (1500);
		else if (Input.GetMouseButtonUp (1))
			CmdThrow (300);
	}

	[Command]
	private void CmdThrow (float strength) {
		var spawnedNade = Instantiate (grenade.DroppedPrefab, look.position + look.forward, Quaternion.Euler (Vector3.forward * 90));
		spawnedNade.GetComponent<Rigidbody> ().AddTorque (Vector3.one * strength);
		spawnedNade.GetComponent<Rigidbody> ().AddForce (look.forward * strength);
		NetworkServer.Spawn (spawnedNade.gameObject);
		spawnedNade.GetComponent<IGrenade> ().Prime (GetComponent<Player> ());
		GetComponent<WeaponManager> ().DeleteCurrentWeapon ();
	}

	[ClientRpc]
	protected void RpcEnable (bool enable) {
		enabled = enable;
	}

}