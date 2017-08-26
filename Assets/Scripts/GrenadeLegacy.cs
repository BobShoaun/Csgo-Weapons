using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Doxel.Utility.ExtensionMethods;

public class GrenadeLegacy : HeldWeapon {

	Player player;
	Transform look;
	WeaponManager wm;

	void Start () {
		player = GetComponentInParent<Player> ();
		look = gameObject.GetGameObjectInParent ("Look").transform;
		wm = GetComponentInParent<WeaponManager> ();
	}

	void Update () {

		if (!player.isLocalPlayer)
			return;

		if (Input.GetMouseButtonUp (0))
			player.CmdThrow (1500);
		else if (Input.GetMouseButtonUp (1))
			player.CmdThrow (300);
	}

	public override void Deploy () {
		
	}

	public void ServerThrow (float strength) {
		player.RpcDeleteWeapon ((int) wm.HoldingWeapon.Weapon.Slot);
		GameObject spawnedNade = Instantiate (DroppedPrefab, look.position + look.forward, Quaternion.Euler (Vector3.forward * 90));
		spawnedNade.GetComponent<Rigidbody> ().AddTorque (Vector3.one * strength);
		spawnedNade.GetComponent<Rigidbody> ().AddForce (look.forward * strength);
		spawnedNade.GetComponent<IGrenade> ().Prime (player);
		NetworkServer.Spawn (spawnedNade);

	}

}