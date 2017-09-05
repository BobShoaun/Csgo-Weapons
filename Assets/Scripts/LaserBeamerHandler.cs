using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Doxel.Utility.ExtensionMethods;

public class LaserBeamerHandler : Handler {

	// Client
	private Transform firstPersonMuzzle;
	private Transform thirdPersonMuzzle;
	private LineRenderer laserPrefab;
	// Server
	private LaserBeamer laserBeamer;

	public override void ServerDeploy (Weapon weapon) {
		if (enabled)
			ServerKeep ();
		laserBeamer = weapon as LaserBeamer;
		if (laserBeamer == null) {
			enabled = false;
			RpcEnable (false);
			return;
		}
		else {
			enabled = true;
			RpcEnable (true);
		}

		RpcCrosshair (laserBeamer.showCrosshair);
		RpcUpdateUI (0, 0, laserBeamer.Name);
	}

	public override void ClientDeploy (GameObject firstPerson, GameObject thirdPerson) {
		if (!enabled)
			return;
		if (isLocalPlayer)
			firstPersonMuzzle = firstPerson.GetGameObjectInChildren ("Muzzle").transform;
		else
			thirdPersonMuzzle = thirdPerson.GetGameObjectInChildren ("Muzzle").transform;
	}

	[ClientRpc]
	protected void RpcEnable (bool enable) {
		enabled = enable;
	}

	protected override void ClientUpdate () {
		if (Input.GetMouseButtonDown (0)) {
			CmdShoot ();
		}
	}

	[Command]
	private void CmdShoot () {
		
	}

	[ClientRpc]
	private void RpcShoot () {
		if (isLocalPlayer)
			Instantiate (laserPrefab).SetPosition (0, firstPersonMuzzle.position);

	}

}