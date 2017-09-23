using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public abstract class Handler : NetworkBehaviour {

	// Local
	[SerializeField]
	private Transform firstPersonMount;
	protected GameObject firstPersonViewmodel;
	// Remote
	[SerializeField]
	private Transform thirdPersonMount;
	protected GameObject thirdPersonWeaponModel;
	// Client
	[SerializeField]
	private WeaponDatabase weaponDatabase;

	// Both
	[SyncVar (hook = "SetWeapon")]
	private int WeaponId = -1;

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
		RpcUpdateAmmo (0);
		RpcUpdateReservedAmmo (0);
	}

	[Client]
	protected virtual void ClientDeploy (Weapon weapon) {
		InstantiateModels (weapon);
		UpdateCrosshair (weapon.showCrosshair);
		UpdateName (weapon.name);
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
	protected void RpcUpdateAmmo (int ammunition) {
		if (!isLocalPlayer)
			return;
		PlayerHUD.Instance.WeaponAmmo = ammunition;
	}

	[ClientRpc]
	protected void RpcUpdateReservedAmmo (int reservedAmmunition) {
		if (!isLocalPlayer)
			return;
		PlayerHUD.Instance.WeaponReserve = reservedAmmunition;
	}

	[Client]
	private void UpdateName (string name) {
		if (!isLocalPlayer)
			return;
		PlayerHUD.Instance.WeaponName = name;
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