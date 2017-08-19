using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using DUtil = Doxel.Utility.Utility;


public class WeaponManager2 : NetworkBehaviour {

	// Client variables
	[SerializeField]
	private Transform weaponHolder;
	[SerializeField]
	private Camera environmentCamera;
	[SerializeField]
	private GameObject bulletHolePrefab;
	private GameObject [] firstPersonWeapons;
	private GameObject [] thirdPersonWeapons;

	// local player variables
	private GunHandler gunHandler;
	private KnifeHandler knifeHandler;

	// Server variables; variables that only exists in the server and is meaningless
	// to all the clients
	[SerializeField]
	private Transform look;
	private DynamicWeapon [] dynamicWeapons;
	private int currentIndex = 2;

	public DynamicGun CurrentDynamicGun {
		get { return dynamicWeapons [currentIndex] as DynamicGun; }
	}

	public Gun2 CurrentGun {
		get { return dynamicWeapons [currentIndex].weapon as Gun2; }
	}

	public Knife2 CurrentKnife {
		get { return dynamicWeapons [currentIndex].weapon as Knife2; }
	}

	private void Start () {
		int weaponSlotAmount = Enum.GetNames (typeof (Weapon.SlotType)).Length;
		if (isLocalPlayer) {
			gunHandler = GetComponent<GunHandler> ();
			knifeHandler = GetComponent<KnifeHandler> ();
		}
		if (isServer) {
			dynamicWeapons = new DynamicWeapon [weaponSlotAmount];
		}
		if (isClient) {
			firstPersonWeapons = new GameObject [weaponSlotAmount];
			thirdPersonWeapons = new GameObject [weaponSlotAmount];
		}
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
			PlayerHUD.Instance.HoverPickup (hit.collider.GetComponent<DroppedWeapon> ().DynamicWeapon.weapon.Name);
			if (Input.GetKeyDown (KeyCode.E))
				CmdEquipWeapon (hit.collider.gameObject);
		}
		else
			PlayerHUD.Instance.HoverDeactivate ();

	}
		
	[Command]
	private void CmdEquipWeapon (GameObject droppedWeapon) {
		DynamicWeapon weapon = droppedWeapon.GetComponent<DroppedWeapon> ().DynamicWeapon;
		int index = (int) weapon.weapon.Slot;
		if (dynamicWeapons [index] != null)
			DropWeapon (index);
		dynamicWeapons [index] = weapon;
		Destroy (droppedWeapon);
		RpcEquipWeapon (weapon.weapon.Id, index);
		SwitchWeapon (index);
	}

	[Server]
	private void DropWeapon (int index) {
		if (dynamicWeapons [index] == null)
			return;
		// DONE instantiate and spawn dropped weapon, then add force
		// assign dynamic weapon info to the dropped weapon
		var droppedWeapon = Instantiate (dynamicWeapons [index].weapon.DroppedPrefab, 
			look.position + look.forward, look.rotation);
		droppedWeapon.GetComponent<DroppedWeapon> ().DynamicWeapon = dynamicWeapons [index];
		droppedWeapon.GetComponent<Rigidbody> ().AddForce (look.forward * 15, ForceMode.Impulse);
		NetworkServer.Spawn (droppedWeapon);
		dynamicWeapons [index] = null;
		RpcDropWeapon (index);
		for (int i = 0; i < dynamicWeapons.Length && !SwitchWeapon (i); i++);
	}

	[Command]
	private void CmdDropCurrentWeapon () {
		DropWeapon (currentIndex);
	}

	[Command]
	private void CmdDropAllWeapons () {
		for (int i = 0; i < dynamicWeapons.Length; i++)
			if (dynamicWeapons [i] != null)
				DropWeapon (i);
	}

	[Server]
	private bool SwitchWeapon (int index) {
		if (index == currentIndex || dynamicWeapons [index] == null)
			return false;
		if (dynamicWeapons [currentIndex] != null)
			RpcSwitch (currentIndex, false);
		RpcSwitch (index, true);
		RpcUpdateHandlers (dynamicWeapons [index].weapon.Id);
		currentIndex = index;
		return true;
	}

	[Command]
	private void CmdSwitchWeapon (int index) {
		SwitchWeapon (index);
	}

	[Command]
	private void CmdScrollSwitchWeapon (int scrollDirection) {
		scrollDirection = Mathf.Clamp (scrollDirection, -1, 1);
		for (int iterations = 0, 
			index = DUtil.Remainder (currentIndex + scrollDirection, dynamicWeapons.Length); 
			iterations < dynamicWeapons.Length; 
			iterations++, index = DUtil.Remainder (index + scrollDirection, dynamicWeapons.Length)) {
			if (dynamicWeapons [index] == null)
				continue;
			SwitchWeapon (index);
			return;
		}
		// No holding weapons
	}

	[ClientRpc]
	private void RpcEquipWeapon (int weaponId, int index) {
		// instantiate weapons based on Id, ui feedback
		//Weapon holdingWeapon = WeaponDatabase.Instance [weaponId];
		firstPersonWeapons [index] = Instantiate (WeaponDatabase.Instance [weaponId].FirstPersonPrefab, weaponHolder);
	}

	[ClientRpc]
	private void RpcDropWeapon (int index) {
		// destroy local hand and viewmodel weapons
		Destroy (firstPersonWeapons [index]);
	}

	[ClientRpc]
	private void RpcSwitch (int index, bool current) {
		firstPersonWeapons [index].SetActive (current);
	}

	[ClientRpc]
	private void RpcUpdateHandlers (int weaponId) {
		if (!isLocalPlayer)
			return;
		knifeHandler.enabled = false;
		gunHandler.enabled = false;
		Weapon2 weapon = WeaponDatabase.Instance [weaponId];
		//if (weapon as Knife2)
			knifeHandler.enabled = weapon as Knife2 != null;
		//if (weapon as Gun2)
			gunHandler.enabled = weapon as Gun2 != null;
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

}