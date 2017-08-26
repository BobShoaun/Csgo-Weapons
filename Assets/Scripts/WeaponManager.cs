using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using DUtil = Doxel.Utility.Utility;

public class WeaponManager : MonoBehaviour {

	[SerializeField]
	HeldWeapon[] weapons;
	int currentIndex = 2;
	Player player;
	Camera cam;

	public HeldWeapon HoldingWeapon {
		get { return weapons [currentIndex]; }
	}

	void Start () {
		weapons = new HeldWeapon[Enum.GetNames (typeof (Weapon.SlotType)).Length];
		player = GetComponentInParent<Player> ();
		cam = GetComponentInParent<Camera> ();
	}

	void Update () {
		if (!player.isLocalPlayer)
			return;
		
		if (Input.GetKeyDown (KeyCode.Alpha1))
			player.CmdSwitch (0);
		else if (Input.GetKeyDown (KeyCode.Alpha2))
			player.CmdSwitch (1);
		else if (Input.GetKeyDown (KeyCode.Alpha3))
			player.CmdSwitch (2);
		else if (Input.GetKeyDown (KeyCode.Alpha4))
			player.CmdSwitch (3);
		else if (Input.GetKeyDown (KeyCode.Alpha5))
			player.CmdSwitch (4);
		else if (Input.GetKeyDown (KeyCode.Alpha6))
			player.CmdSwitch (5);
		else if (Input.GetKeyDown (KeyCode.Alpha7))
			player.CmdSwitch (6);
		else if (Input.GetKeyDown (KeyCode.Alpha8))
			player.CmdSwitch (7);

		if (Input.GetKeyDown (KeyCode.Q))
			player.CmdDropWeapon (currentIndex);

		if (Input.GetAxis ("Mouse ScrollWheel") < 0)
			player.CmdScrollSwitch (1);
		else if (Input.GetAxis ("Mouse ScrollWheel") > 0)
			player.CmdScrollSwitch (-1);

		RaycastHit hit;
		if (Physics.Raycast (cam.ViewportPointToRay (Vector2.one * 0.5f), out hit, 5)
		    && hit.collider.CompareTag ("Weapon")) {
			// rmb this is still called in local player, if check is above
			//PlayerHUD.Instance.HoverPickup (hit.collider.GetComponent<DroppedWeapon> ().Weapon.Name);
			if (Input.GetKeyDown (KeyCode.E)) {
				player.CmdPickupWeapon (hit.collider.gameObject);
			}
		}
		else {
			PlayerHUD.Instance.HoverDeactivate ();
		}
	}

	public void ServerPickupWeapon (GameObject weapon) {
		//int index = (int) weapon.GetComponent<DroppedWeapon> ().Weapon.Slot;
		//if (weapons [index])
		//	ServerDropWeapon (index);
		player.RpcPickupWeapon (weapon);
	}

	public void ClientPickupWeapon (GameObject weapon) {
		//HeldWeapon newWeapon = Instantiate (weapon.GetComponent<DroppedWeapon> ().HeldPrefab, transform).GetComponent<HeldWeapon> ();
		//int index = (int) newWeapon.Weapon.Slot;
		//weapons [index] = newWeapon;
		//player.CmdSwitch (index);
		player.CmdDespawn (weapon);
	}
		
	public void ServerDropWeapon (int index) {
		// return if the index is invalid
		if (!weapons [index])
			return;
		var droppedWeapon = Instantiate (weapons [index].DroppedPrefab, 
			weapons [index].transform.position, weapons [index].transform.rotation);
		droppedWeapon.GetComponent<Rigidbody> ().AddForce (transform.forward * 10, ForceMode.Impulse);
		NetworkServer.Spawn (droppedWeapon);
		player.RpcDropWeapon (index);
	}

	public void ServerDropAllWeapons () {
		for (int i = 0; i < weapons.Length; i++)
			if (weapons [i])
				ServerDropWeapon (i);
	}
		
	public void ClientDeleteWeapon (int index) {
		Destroy (weapons [index].gameObject);
		weapons [index] = null;
		for (int i = 0; i < weapons.Length && !ClientSwitch (i); i++);
	}

	public bool ClientSwitch (int index) {
		// DONE check if switching the same weapon, if so dont
		// deactivate it
		if (index == currentIndex)
			return false;
		// check if the index is valid
		if (!weapons [index])
			return false;
		if (HoldingWeapon)
			HoldingWeapon.gameObject.SetActive (false);
		currentIndex = index;
		HoldingWeapon.gameObject.SetActive (true);
		HoldingWeapon.Deploy ();
		if (player.isLocalPlayer) {
			GunLegacy gun = weapons [currentIndex] as GunLegacy;
			PlayerHUD.Instance.WeaponName = HoldingWeapon.Weapon.Name;
			PlayerHUD.Instance.WeaponAmmo = gun ? gun.AmmunitionInMagazine : 0;
			PlayerHUD.Instance.WeaponReserve = gun ? gun.ReservedAmmunition : 0;
		}
		return true;
	}
		
	public void ClientScrollSwitch (int direction) {
		// DONE when switching weapons dont deactivate all the weapon
		// gameobjects, just deactivate the nessasary onces
		direction = Mathf.Clamp (direction, -1, 1);
		// for loop the runs as long as the weapons length, 
		// but starts frm the current index and increases or decreases
		// depending on the direction given
		for (int j = 0, 
			i = DUtil.Remainder (currentIndex + direction, weapons.Length); 
			j < weapons.Length; 
			j++, i = DUtil.Remainder (i + direction, weapons.Length)) {
			if (!weapons [i])
				continue;
			ClientSwitch (i);
			return;
		}
		// not holding any weapon
		if (player.isLocalPlayer) {
			PlayerHUD.Instance.WeaponName = string.Empty;
			PlayerHUD.Instance.WeaponAmmo = 0;
			PlayerHUD.Instance.WeaponReserve = 0;
		}

	}

}