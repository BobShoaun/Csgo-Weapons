using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public abstract class Handler : NetworkBehaviour {

	private void OnEnable () {
//		if (isServer)
//			ServerDeploy ();
//		if (isClient)
//			ClientDeploy ();
	}

	private void OnDisable () {
		if (isServer)
			ServerKeep ();
		if (isClient)
			ClientKeep ();
	}

	private void Update () {
		if (isServer)
			ServerUpdate ();
		if (isClient)
			ClientUpdate ();
	}

	[Server]
	public virtual void ServerDeploy (Weapon weapon) {}

	[Client]
	public virtual void ClientDeploy (GameObject firstPerson, GameObject thirdPerson) {}

	[Server]
	public virtual void ServerKeep () {
		enabled = false;
	}

	protected virtual void ClientKeep (){}

	protected virtual void ServerUpdate (){}

	protected virtual void ClientUpdate (){}

	[ClientRpc]
	protected void RpcUpdateUI (int ammo, int reserved, string name) {
		if (!isLocalPlayer)
			return;
		PlayerHUD.Instance.WeaponName = name;
		PlayerHUD.Instance.WeaponAmmo = ammo;
		PlayerHUD.Instance.WeaponReserve = reserved;
	}

	[ClientRpc]
	protected void RpcCrosshair (bool active) {
		if (!isLocalPlayer)
			return;
		PlayerHUD.Instance.crossHair.SetActive (active);
	}

}