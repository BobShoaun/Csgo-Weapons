using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Grenade : HeldWeapon {

	Player player;
	Camera cam;
	WeaponManager wm;

	void Start () {
		player = GetComponentInParent<Player> ();
		cam = GetComponentInParent<Camera> ();
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
		GameObject spawnedNade = Instantiate (DroppedPrefab, transform.position, transform.rotation);
		spawnedNade.GetComponent<Rigidbody> ().AddForce (cam.transform.forward * strength);
		spawnedNade.GetComponent<IGrenade> ().Prime (player);
		NetworkServer.Spawn (spawnedNade);

	}

}