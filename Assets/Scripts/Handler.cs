using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public abstract class Handler : NetworkBehaviour {

	private Weapon weapon;

	[SyncVar (hook = "SetEnable")]
	protected bool Enabled = false;

	private void Update () {
		if (isServer)
			ServerUpdate ();
		if (isClient)
			ClientUpdate ();
	}
		
	private void SetEnable (bool enabled) {
		this.enabled = enabled;
		if (enabled)
			ServerDeploy ();
	}

	[Server]
	public void OnWeaponChanged (Weapon weapon) {
		if (enabled)
			ServerKeep ();
		Enabled = SetWeapon (weapon);
	}

	[Client]
	public void OnModelChanged (GameObject firstPerson, GameObject thirdPerson) {
		if (enabled)
			ClientDeploy (firstPerson, thirdPerson);
	}

	[Server]
	protected virtual bool SetWeapon (Weapon weapon) {
		this.weapon = weapon;
		return weapon != null;
	}

	[Server]
	protected virtual void ServerDeploy () {
		RpcCrosshair (weapon.showCrosshair);
		RpcUpdateUI (0, 0, weapon.name);
	}

	[Client]
	protected abstract void ClientDeploy (GameObject firstPerson, GameObject thirdPerson);

	[Server]
	protected abstract void ServerKeep ();

	[Client]
	protected virtual void ClientKeep () { }

	[ServerCallback]
	protected virtual void ServerUpdate () { }

	[ClientCallback]
	protected abstract void ClientUpdate ();

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