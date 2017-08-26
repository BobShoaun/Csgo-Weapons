﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GrenadeHandler : Handler {

	// Client


	// Server
	public Transform look;
	private Grenade grenade;

	[ServerCallback]
	private void OnEnable () {
		grenade = GetComponent<WeaponManager2> ().CurrentWeapon as Grenade;
		RpcUpdateUI (0, 0, grenade.Name);
	}

	private void OnDisable () {
		
	}

	[ClientCallback]
	private void Update () {
		if (!isLocalPlayer)
			return;
		
		if (Input.GetMouseButtonUp (0))
			CmdThrow (1500);
		else if (Input.GetMouseButtonUp (1))
			CmdThrow (300);
	}

	[Command]
	private void CmdThrow (float strength) {
		GetComponent<WeaponManager2> ().DeleteCurrentWeapon ();
		GameObject spawnedNade = Instantiate (grenade.DroppedPrefab, look.position + look.forward, Quaternion.Euler (Vector3.forward * 90));
		spawnedNade.GetComponent<Rigidbody> ().AddTorque (Vector3.one * strength);
		spawnedNade.GetComponent<Rigidbody> ().AddForce (look.forward * strength);
		NetworkServer.Spawn (spawnedNade);
		spawnedNade.GetComponent<IGrenade> ().Prime (GetComponent<Player> ());
	}

	[ClientRpc]
	private void RpcUpdateUI (int ammo, int reserved, string name) {
		if (!isLocalPlayer)
			return;
		PlayerHUD.Instance.WeaponName = name;
		PlayerHUD.Instance.WeaponAmmo = ammo;
		PlayerHUD.Instance.WeaponReserve = reserved;
	}

}