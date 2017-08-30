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

	protected override void ServerDeploy () {
		launcher = GetComponent<WeaponManager> ().CurrentWeapon as Launcher;
		nextFireTime = Time.time + launcher.deployDuration;
	}

	protected override void ClientDeploy () {
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