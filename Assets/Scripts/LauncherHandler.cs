using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Doxel.Utility.ExtensionMethods;

public class LauncherHandler : Handler {

	// Client
	private Transform muzzle;

	// Server
	[SerializeField]
	private Transform look;
	private Launcher launcher;
	private float nextFireTime = 0;

	public override void ServerDeploy (Weapon weapon) {
		if (enabled)
			ServerKeep ();
		launcher = weapon as Launcher;
		if (launcher == null) {
			enabled = false;
			RpcEnable (false);
			return;
		}
		else {
			enabled = true;
			RpcEnable (true);
		}
		nextFireTime = Time.time + launcher.deployDuration;
	}

	public override void ClientDeploy (GameObject firstPerson, GameObject thirdPerson) {
		if (!enabled)
			return;
		muzzle = firstPerson.GetGameObjectInChildren ("Muzzle").transform;
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
		RaycastHit hit;
		if (Physics.Raycast (look.position, look.forward, out hit)) {
			Debug.DrawRay (muzzle.position, hit.point - muzzle.position);
			NetworkServer.Spawn (Instantiate (launcher.projectilePrefab, 
				muzzle.position, Quaternion.LookRotation ((hit.point - muzzle.position).normalized)));
			//Quaternion.LookRotation (hit.point - muzzle.position);
			//Quaternion.LookRotation (muzzle.up, muzzle.forward) 


		}

	}

	[ClientRpc]
	protected void RpcEnable (bool enable) {
		enabled = enable;
	}

}