using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using DUtil = Doxel.Utility.Utility;

public class WeaponManager2 : NetworkBehaviour {
	
	[SerializeField]
	private Transform weaponHolder;
	[SerializeField]
	private Camera environmentCamera;
	// Server variables; variables that only exists in the server and is meaningless
	// in all the clients
	private HeldWeapon[] weapons;
	private int currentIndex = 2;

	[ServerCallback]
	private void Start () {
		weapons = new HeldWeapon [Enum.GetNames (typeof (Weapon.SlotType)).Length];
	}

	[ClientCallback]
	private void Update () {
		if (!isLocalPlayer)
			return;
		if (Input.GetKeyDown (KeyCode.Alpha1))
			SwitchWeapon (0);
		else if (Input.GetKeyDown (KeyCode.Alpha2))
			SwitchWeapon (1);
		else if (Input.GetKeyDown (KeyCode.Alpha3))
			SwitchWeapon (2);
		else if (Input.GetKeyDown (KeyCode.Alpha4))
			SwitchWeapon (3);
		else if (Input.GetKeyDown (KeyCode.Alpha5))
			SwitchWeapon (4);
		else if (Input.GetKeyDown (KeyCode.Alpha6))
			SwitchWeapon (5);
		else if (Input.GetKeyDown (KeyCode.Alpha7))
			SwitchWeapon (6);
		else if (Input.GetKeyDown (KeyCode.Alpha8))
			SwitchWeapon (7);

		if (Input.GetKeyDown (KeyCode.Q))
			CmdDropCurrentWeapon ();

		if (Input.GetAxis ("Mouse ScrollWheel") < 0)
			CmdScrollSwitchWeapon (1);
		else if (Input.GetAxis ("Mouse ScrollWheel") > 0)
			CmdScrollSwitchWeapon (-1);

		RaycastHit hit;
		if (Physics.Raycast (environmentCamera.ViewportPointToRay (Vector2.one * 0.5f), 
			out hit, 5) && hit.collider.CompareTag ("Weapon")) {
			// rmb this is still called in local player, if check is above
			PlayerHUD.Instance.HoverPickup (hit.collider.GetComponent<DroppedWeapon> ().Weapon.Name);
			if (Input.GetKeyDown (KeyCode.E))
				CmdEquipWeapon (hit.collider.gameObject);
		}
		else
			PlayerHUD.Instance.HoverDeactivate ();
	}

	[Command]
	private void CmdEquipWeapon (GameObject droppedWeapon) {
		Weapon weapon = droppedWeapon.GetComponent<DroppedWeapon> ().Weapon;
		int index = (int) weapon.Slot;
		if (weapons [index])
			DropWeapon (index);
		HeldWeapon heldWeapon = Instantiate (droppedWeapon.GetComponent<DroppedWeapon> ().HeldPrefab, weaponHolder).GetComponent<HeldWeapon> ();
		Destroy (droppedWeapon);
		weapons [index] = heldWeapon;
		RpcEquipWeapon (heldWeapon.gameObject, index);
	}

	[ClientRpc]
	private void RpcEquipWeapon (GameObject heldWeapon, int index) {
		if (!isServer)
			Instantiate (heldWeapon, weaponHolder);
		CmdSwitchWeapon (index);
	}

	[Server]
	private void DropWeapon (int index) {
		if (!weapons [index])
			return;
		var droppedWeapon = Instantiate (weapons [index].DroppedPrefab, 
			weapons [index].transform.position, weapons [index].transform.rotation);
		droppedWeapon.GetComponent<Rigidbody> ().AddForce (transform.forward * 10, ForceMode.Impulse);
		NetworkServer.Spawn (droppedWeapon);
		Destroy (weapons [index].gameObject);
		weapons [index] = null;
		for (int i = 0; i < weapons.Length && !SwitchWeapon (i); i++);
	}

	[Command]
	private void CmdDropCurrentWeapon () {
		DropWeapon (currentIndex);
	}

	[Command]
	private void CmdDropAllWeapons () {
		for (int i = 0; i < weapons.Length; i++)
			if (weapons [i])
				DropWeapon (i);
	}

	[ClientRpc]
	private void RpcSetActive (GameObject gameObject, bool active) {
		gameObject.SetActive (active);
	}

	[ClientRpc]
	private void RpcUpdateWeaponUI (GameObject weaponGO) {
		if (!isLocalPlayer)
			return;
		if (weaponGO) {
			HeldWeapon weapon = weaponGO.GetComponent<HeldWeapon> ();
			PlayerHUD.Instance.WeaponName = weapon.Weapon.Name;
			Gun gun = weapon as Gun;
			PlayerHUD.Instance.WeaponAmmo = gun ? gun.AmmunitionInMagazine : 0;
			PlayerHUD.Instance.WeaponReserve = gun ? gun.ReservedAmmunition : 0;
		}
		else {
			PlayerHUD.Instance.WeaponName = string.Empty;
			PlayerHUD.Instance.WeaponAmmo = 0;
			PlayerHUD.Instance.WeaponReserve = 0;
		}
	}

	[Server]
	private bool SwitchWeapon (int index) {
		if (index == currentIndex || !weapons [index])
			return false;
		if (weapons [currentIndex])
			RpcSetActive (weapons [currentIndex].gameObject, false);
		RpcSetActive (weapons [index].gameObject, true);
		currentIndex = index;
		RpcUpdateWeaponUI (weapons [index].gameObject);
		return true;
	}

	[Command]
	private void CmdSwitchWeapon (int index) {
		SwitchWeapon (index);
	}

	[Command]
	private void CmdScrollSwitchWeapon (int scrollDirection) {
		scrollDirection = Mathf.Clamp (scrollDirection, -1, 1);
		for (int j = 0, 
			i = DUtil.Remainder (currentIndex + scrollDirection, weapons.Length); 
			j < weapons.Length; 
			j++, i = DUtil.Remainder (i + scrollDirection, weapons.Length)) {
			if (!weapons [i])
				continue;
			SwitchWeapon (i);
			return;
		}
		RpcUpdateWeaponUI (null);
	}

}