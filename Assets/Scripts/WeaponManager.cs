using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using DUtil = Doxel.Utility.Utility;


public class WeaponManager : NetworkBehaviour {

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


	private GameObject firstPersonWeapon;
	private GameObject thirdPersonWeapon;

	// both client and server
	private Handler[] handlers;
	private int currentIndex = 2;

	// Server variables; variables that only exists in the server and is meaningless
	// to all the clients
	[SerializeField]
	private Transform look;
	private Weapon [] weapons;

	public event Action<Weapon, Weapon> OnWeaponChanged;

	public Weapon CurrentWeapon {
		get { return weapons [currentIndex]; }
	}

	private void Start () {
		int weaponSlotAmount = Enum.GetNames (typeof (Weapon.SlotType)).Length;
		handlers = GetComponents<Handler> ();
		if (isServer) {
			weapons = new Weapon [weaponSlotAmount];
		}
		if (isClient) {
		}
	}

	[ClientCallback]
	private void Update () {
		if (!isLocalPlayer)
			return;
		if (Input.GetKeyDown (KeyCode.Alpha1))
			CmdSwitchWeapon (0);
		else if (Input.GetKeyDown (KeyCode.Alpha2))
			CmdSwitchWeapon (1);
		else if (Input.GetKeyDown (KeyCode.Alpha3))
			CmdSwitchWeapon (2);
		else if (Input.GetKeyDown (KeyCode.Alpha4))
			CmdSwitchWeapon (3);
		else if (Input.GetKeyDown (KeyCode.Alpha5))
			CmdSwitchWeapon (4);
		else if (Input.GetKeyDown (KeyCode.Alpha6))
			CmdSwitchWeapon (5);
		else if (Input.GetKeyDown (KeyCode.Alpha7))
			CmdSwitchWeapon (6);
		else if (Input.GetKeyDown (KeyCode.Alpha8))
			CmdSwitchWeapon (7);

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
		if (!SwitchWeapon (index)) {
			foreach (var handler in handlers)
				handler.ServerDeploy (weapon);
			RpcInstantiateViewmodel (weapon.Id);
		}
	}

	[Server]
	public void DeleteCurrentWeapon () {
		DeleteWeapon (currentIndex);
	}

	[Server]
	private void DeleteWeapon (int index) {
		weapons [index] = null;
		RpcDestroyViewmodel ();
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

	[Server]
	public void DropAllWeapons () {
		for (int i = 0; i < weapons.Length; i++)
			if (weapons [i] != null)
				DropWeapon (i);
	}

	[Server]
	private bool SwitchWeapon (int index) {
		if (index == currentIndex || weapons [index] == null)
			return false;
		if (weapons [currentIndex] != null) {
			//RpcSwitch (currentIndex, false);
		}
		currentIndex = index;

		foreach (Handler handler in handlers)
			handler.ServerDeploy (weapons [index]);
		RpcInstantiateViewmodel (weapons [index].Id);
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

	[ClientRpc]
	private void RpcInstantiateViewmodel (int weaponId) {
		if (isLocalPlayer) {
			Destroy (firstPersonWeapon);
			firstPersonWeapon = Instantiate (weaponDatabase [weaponId].FirstPersonPrefab, weaponHolder);
		}
		else {
			Destroy (thirdPersonWeapon);
			thirdPersonWeapon = Instantiate (weaponDatabase [weaponId].ThirdPersonPrefab);
			thirdPersonWeapon.transform.SetParent (weaponMount, true);
			thirdPersonWeapon.transform.localPosition = Vector3.zero;
			thirdPersonWeapon.transform.localRotation = Quaternion.identity;
		}
		foreach (Handler handler in handlers)
			handler.ClientDeploy (firstPersonWeapon, thirdPersonWeapon);
	}

	[ClientRpc]
	private void RpcDestroyViewmodel () {
		Destroy (isLocalPlayer ? firstPersonWeapon : thirdPersonWeapon);
	}

	[ServerCallback]
	private void OnControllerColliderHit (ControllerColliderHit controllerColliderHit) {
		if (controllerColliderHit.collider.CompareTag ("Weapon"))
			if (weapons [(int) controllerColliderHit.collider.GetComponent<DroppedWeapon> ().weapon.Slot] == null)
				CmdEquipWeapon (controllerColliderHit.collider.gameObject);
	}

}