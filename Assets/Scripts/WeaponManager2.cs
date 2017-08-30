using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using DUtil = Doxel.Utility.Utility;


public class WeaponManager2 : NetworkBehaviour {

	// Client variables
	public WeaponDatabase weaponDatabase;
	[SerializeField]
	private Transform weaponHolder;
	[SerializeField]
	private Transform weaponMount;
	[SerializeField]
	private Camera environmentCamera;
	[SerializeField]
	private GameObject bulletHolePrefab;
	private GameObject [] firstPersonWeapons;
	private GameObject [] thirdPersonWeapons;

	public GameObject HoldingWeapon {
		get { return firstPersonWeapons [currentIndex]; }
	}

	// both client and server
	private GunHandler gunHandler;
	private KnifeHandler knifeHandler;
	private GrenadeHandler grenadeHandler;
	private LauncherHandler launcherHandler;
	private int currentIndex = 2;

	// Server variables; variables that only exists in the server and is meaningless
	// to all the clients
	[SerializeField]
	private Transform look;
	private Weapon [] weapons;

	public Weapon CurrentWeapon {
		get { return weapons [currentIndex]; }
	}

	private void Start () {
		int weaponSlotAmount = Enum.GetNames (typeof (Weapon.SlotType)).Length;
		gunHandler = GetComponent<GunHandler> ();
		knifeHandler = GetComponent<KnifeHandler> ();
		grenadeHandler = GetComponent<GrenadeHandler> ();
		launcherHandler = GetComponent<LauncherHandler> ();
		if (isServer) {
			weapons = new Weapon [weaponSlotAmount];
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
			CmdCycleSwitchWeapon (1);
		else if (Input.GetAxis ("Mouse ScrollWheel") > 0)
			CmdCycleSwitchWeapon (-1);

		RaycastHit hit;
		if (Physics.Raycast (environmentCamera.ViewportPointToRay (Vector2.one * 0.5f), 
			out hit, 5) && hit.collider.CompareTag ("Weapon")) {
			// rmb this is still called in local player, if check is above
			PlayerHUD.Instance.HoverPickup (hit.collider.GetComponent<DroppedWeapon> ().name);
			if (Input.GetKeyDown (KeyCode.E))
				CmdEquipWeapon (hit.collider.gameObject);
		}
		else
			PlayerHUD.Instance.HoverDeactivate ();
	}
		
	[Command]
	private void CmdEquipWeapon (GameObject droppedWeapon) {
		Weapon weapon = droppedWeapon.GetComponent<DroppedWeapon> ().weapon;
		Destroy (droppedWeapon);
		int index = (int) weapon.Slot;
		if (weapons [index] != null)
			DropWeapon (index);
		weapons [index] = weapon;
		RpcEquipWeapon (weapon.Id, index);
		SwitchWeapon (index);
	}

	[Server]
	public void DeleteCurrentWeapon () {
		DeleteWeapon (currentIndex);
	}

	[Server]
	private void DeleteWeapon (int index) {
		weapons [index] = null;
		RpcDeleteWeapon (index);
		for (int i = 0; i < weapons.Length && !SwitchWeapon (i); i++);
	}

	[Server]
	private void DropWeapon (int index) {
		if (weapons [index] == null)
			return;
		// DONE instantiate and spawn dropped weapon, then add force
		// assign dynamic weapon info to the dropped weapon
		var droppedWeapon = Instantiate (weapons [index].DroppedPrefab, 
			look.position + look.forward, look.rotation);
		droppedWeapon.GetComponent<DroppedWeapon> ().weapon = weapons [index];
		droppedWeapon.GetComponent<Rigidbody> ().AddForce (look.forward * 15, ForceMode.Impulse);
		NetworkServer.Spawn (droppedWeapon);
		DeleteWeapon (index);
	}

	[Command]
	private void CmdDropCurrentWeapon () {
		DropWeapon (currentIndex);
	}

	[Command]
	private void CmdDropAllWeapons () {
		for (int i = 0; i < weapons.Length; i++)
			if (weapons [i] != null)
				DropWeapon (i);
	}

	[Server]
	private bool SwitchWeapon (int index) {
		if (index == currentIndex || weapons [index] == null)
			return false;
		if (weapons [currentIndex] != null)
			RpcSwitch (currentIndex, false);
		RpcSwitch (index, true);
		currentIndex = index;
		//UpdateWeaponHandlers ();
		return true;
	}

	[Command]
	private void CmdSwitchWeapon (int index) {
		SwitchWeapon (index);
	}

	[Command]
	private void CmdCycleSwitchWeapon (int cycleDirection) {
		cycleDirection = Mathf.Clamp (cycleDirection, -1, 1);
		for (int iterations = 0, 
			index = DUtil.Remainder (currentIndex + cycleDirection, weapons.Length); 
			iterations < weapons.Length; 
			iterations++, index = DUtil.Remainder (index + cycleDirection, weapons.Length)) {
			if (weapons [index] == null)
				continue;
			SwitchWeapon (index);
			return;
		}
		// No holding weapons
	}

	[Command]
	private void CmdUpdateWeaponHandlers () {
		knifeHandler.Keep ();
		gunHandler.enabled = knifeHandler.enabled = grenadeHandler.enabled = false;
		int num = 0;
		if (CurrentWeapon as Gun != null) {
			//gunHandler.enabled = true;
			num = 1;
		}
		else if (CurrentWeapon as Knife != null) {
			//knifeHandler.enabled = true;
			num = 2;
		}
		else if (CurrentWeapon as Grenade != null) {
			//grenadeHandler.enabled = true;
			num = 3;
		}
		else if (CurrentWeapon as Launcher != null) {
			num = 4;
		}
		RpcUpdateHandlers (num);
	}

	[ClientRpc]
	private void RpcEquipWeapon (int weaponId, int index) {
		// instantiate weapons based on Id, ui feedback
		//Weapon holdingWeapon = WeaponDatabase.Instance [weaponId];
		currentIndex = index;
		if (isLocalPlayer) {
			firstPersonWeapons [index] = Instantiate (weaponDatabase [weaponId].FirstPersonPrefab, weaponHolder);
		}
		else {
			thirdPersonWeapons [index] = Instantiate (weaponDatabase [weaponId].ThirdPersonPrefab);
			thirdPersonWeapons [index].transform.SetParent (weaponMount, true);
			thirdPersonWeapons [index].transform.localPosition = Vector3.zero;
			thirdPersonWeapons [index].transform.localRotation = Quaternion.identity;
		}
		CmdUpdateWeaponHandlers ();
	}

	[ClientRpc]
	private void RpcDeleteWeapon (int index) {
		// destroy local hand and viewmodel weapons
		if (isLocalPlayer) 
			Destroy (firstPersonWeapons [index]);
		else 
			Destroy (thirdPersonWeapons [index]);
		CmdUpdateWeaponHandlers ();
	}

	[ClientRpc]
	private void RpcSwitch (int index, bool current) {
		if (current)
			currentIndex = index;
		if (isLocalPlayer)
			firstPersonWeapons [index].SetActive (current);
		else
			thirdPersonWeapons [index].SetActive (current);
		CmdUpdateWeaponHandlers ();
	}

	[ClientRpc]
	private void RpcUpdateHandlers (int num) {
		gunHandler.enabled = num == 1;
		knifeHandler.enabled = num == 2;
		grenadeHandler.enabled = num == 3;
		launcherHandler.enabled = num == 4;
	}

	[ServerCallback]
	private void OnControllerColliderHit (ControllerColliderHit controllerColliderHit) {
		if (controllerColliderHit.collider.CompareTag ("Weapon"))
			if (weapons [(int) controllerColliderHit.collider.GetComponent<DroppedWeapon> ().weapon.Slot] == null)
				CmdEquipWeapon (controllerColliderHit.collider.gameObject);
	}

}