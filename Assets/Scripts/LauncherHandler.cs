using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Doxel.Utility.ExtensionMethods;

public class LauncherHandler : Handler {

	// Client
	private Transform muzzle;
	// Server
	private Launcher launcher;
	private float nextFireTime = 0;

	public override void ServerDeploy (Weapon weapon) {
		launcher = weapon as Launcher;
		if (launcher == null) {
			enabled = false;
			return;
		}
		else
			enabled = true;
		nextFireTime = Time.time + launcher.deployDuration;
	}

	public override void ClientDeploy (GameObject firstPerson, GameObject thirdPerson) {
		muzzle = GetComponent<WeaponManager> ().HoldingWeapon.GetGameObjectInChildren ("Muzzle").transform;
	}

	protected override void ClientUpdate () {
		if (!isLocalPlayer)
			return;

		if (Input.GetMouseButtonDown (0))
			CmdFire ();
	}

	[Command]
	private void CmdFire () {
		if (Time.time < nextFireTime)
			return;
		nextFireTime = Time.time + 1 / launcher.fireRate;
		print ("launch");
		NetworkServer.Spawn (Instantiate (launcher.projectilePrefab, 
			muzzle.position, Quaternion.LookRotation (muzzle.up, muzzle.forward)));
	}

}