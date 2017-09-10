using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public abstract class Handler : NetworkBehaviour {

	// Client
	[SerializeField]
	private WeaponDatabase weaponDatabase;
	[SerializeField]
	private Transform firstPersonMount;
	[SerializeField]
	private Transform thirdPersonMount;
	protected GameObject firstPersonViewmodel;
	protected GameObject thirdPersonWeaponModel;

	[SyncVar (hook = "SetWeapon")]
	private int WeaponId;

	// Server
	private Weapon weapon;

	protected abstract Type WeaponType { get; }

	private void Update () {
		if (isServer)
			ServerUpdate ();
		if (isClient)
			ClientUpdate ();
	}

	private void SetWeapon (int id) {
		if (id == -1) {
			enabled = false;
			if (isClient)
				ClientKeep ();
		}
		else {
			enabled = true;
			if (isServer)
				ServerDeploy (weapon);
			if (isClient)
				ClientDeploy (weaponDatabase [id]);
		}
	}

	[Server]
	public void OnWeaponChanged (Weapon weapon) {
		if (enabled)
			ServerKeep ();
		if (weapon == null || WeaponType != weapon.GetType ())
			WeaponId = -1;
		else {
			this.weapon = weapon;
			WeaponId = weapon.Id;
		}
	}

	[Server]
	protected virtual void ServerDeploy (Weapon weapon) {
		RpcUpdateUI (0, 0, weapon.name);
	}

	[Client]
	protected virtual void ClientDeploy (Weapon weapon) {
		InstantiateModels (weapon);
		UpdateCrosshair (weapon.showCrosshair);
	}

	[Server]
	protected abstract void ServerKeep ();

	[Client]
	protected virtual void ClientKeep () {
		DestroyModels ();
	}

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
	protected void RpcUpdateCrosshair (bool active) {
		UpdateCrosshair (active);
	}

	[Client]
	private void UpdateCrosshair (bool active) {
		if (!isLocalPlayer)
			return;
		PlayerHUD.Instance.crossHair.SetActive (active);
	}

	[Client]
	private void InstantiateModels (Weapon weapon) {
		if (firstPersonViewmodel || thirdPersonWeaponModel)
			DestroyModels ();
		if (isLocalPlayer) {
			firstPersonViewmodel = Instantiate (weapon.FirstPersonPrefab, firstPersonMount);
		}
		else {
			thirdPersonWeaponModel = Instantiate (weapon.ThirdPersonPrefab);
			thirdPersonWeaponModel.transform.SetParent (thirdPersonMount, true);
			thirdPersonWeaponModel.transform.localPosition = Vector3.zero;
			thirdPersonWeaponModel.transform.localRotation = Quaternion.identity;
		}
	}

	[Client]
	private void DestroyModels () {
		Destroy (isLocalPlayer ? firstPersonViewmodel : thirdPersonWeaponModel);
	}

}