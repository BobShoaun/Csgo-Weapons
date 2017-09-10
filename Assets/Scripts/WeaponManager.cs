using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using DUtil = Doxel.Utility.Utility;


public class WeaponManager : NetworkBehaviour {

	// Client
	[SerializeField]
	private Camera environmentCamera;

	// Server ; variables that only exists in the server and is meaningless
	// to all the clients
	private Handler[] handlers;
	[SerializeField]
	private Transform look;
	private Weapon [] weapons;
	private int currentIndex = 2;

	[ServerCallback]
	private void Start () {
		handlers = GetComponents<Handler> ();
		weapons = new Weapon [Enum.GetNames (typeof (Weapon.SlotType)).Length];
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
			PlayerHUD.Instance.HoverPickup (hit.collider.name);
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
			print ("CALLED");
			Array.ForEach (handlers, handler => handler.OnWeaponChanged (weapons [index]));
			//RpcInstantiateViewmodel (weapon.Id);
		}
	}

	[Server]
	public void DeleteCurrentWeapon () {
		DeleteWeapon (currentIndex);
	}

	[Server]
	private void DeleteWeapon (int index) {
		weapons [index] = null;
		//RpcDestroyViewmodel ();
		//for (int i = 0; i < weapons.Length && !SwitchWeapon (i); i++);
		for (int i = 0; i < weapons.Length; i++)
			if (SwitchWeapon (i))
				return;
		print ("CALLED");
		Array.ForEach (handlers, handler => handler.OnWeaponChanged (weapons [index]));
	}

	[Server]
	private void DropWeapon (int index) {
		if (weapons [index] == null)
			return;
		// DONE instantiate and spawn dropped weapon, then add force
		// assign dynamic weapon info to the dropped weapon
		var droppedWeapon = Instantiate (weapons [index].DroppedPrefab, 
			look.position + look.forward, look.rotation);
		droppedWeapon.weapon = weapons [index];
		droppedWeapon.GetComponent<Rigidbody> ().AddForce 
		(GetComponent<CharacterController> ().velocity + (look.forward * 15), ForceMode.Impulse);
		NetworkServer.Spawn (droppedWeapon.gameObject);
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
		if (index == currentIndex || weapons [index] == null) {
			return false;
		}
		print ("CALLED");
		Array.ForEach (handlers, handler => handler.OnWeaponChanged (weapons [index]));
		//RpcInstantiateViewmodel (weapons [index].Id);
		currentIndex = index;
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

	[ServerCallback]
	private void OnControllerColliderHit (ControllerColliderHit controllerColliderHit) {
		if (controllerColliderHit.collider.CompareTag ("Weapon"))
			if (weapons [(int) controllerColliderHit.collider.GetComponentInParent<DroppedWeapon> ().weapon.Slot] == null)
				CmdEquipWeapon (controllerColliderHit.collider.gameObject);
	}

}