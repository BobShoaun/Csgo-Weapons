using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityRandom = UnityEngine.Random;
using DUtil = Doxel.Utility.Utility;

public class GunHandler : NetworkBehaviour {

	// client
	public View view;
	public Transform muzzle;
	public GameObject bulletHolePrefab;

	// server 
	public Transform recoilTransform;
	private float nextFireTime = 0;
	private float nextContinuousReloadTime = 0;
	private float nextRecoilCooldownTime = 0;
	private bool reloading = false;
	private float innacuracy = 0;
	private WeaponManager2 wm;
	private Vector3 recoilRotation;

	[ServerCallback]
	private void Start () {
		wm = GetComponent<WeaponManager2> ();
	}

	private void Update () {
		if (isServer)
			RecoilCooldown ();
		if (!isLocalPlayer)
			return;
		if (Input.GetKeyDown (KeyCode.R))
			CmdReload ();

		if (Input.GetMouseButton (0))
			CmdFire (Input.GetMouseButtonDown (0));

		if (Input.GetMouseButtonUp (0))
			CmdEmptyReload ();
	}

	[Command]
	private void CmdFire (bool mouseDown) {

		if (!mouseDown && !wm.CurrentGun.continuousFire)
			return;
		if (reloading)
			return;
		if (Time.time < nextFireTime)
			return;
		if (wm.CurrentDynamicGun.ammunitionInMagazine <= 0)
			return;
		nextFireTime = Time.time + 1 / wm.CurrentGun.fireRate;
		wm.CurrentDynamicGun.ammunitionInMagazine--;
		if (!wm.CurrentGun.recoil.MoveNext ())
			wm.CurrentGun.recoil.Reset ();

		for (int i = 0; i < wm.CurrentGun.bulletsPerShot; i++) {
			innacuracy += wm.CurrentGun.accuracyDecay;
			nextRecoilCooldownTime = Time.time + wm.CurrentGun.recoilCooldown;
			var recoilRotation = new Vector3 (-wm.CurrentGun.recoil.Current.y,
				                     wm.CurrentGun.recoil.Current.x) * wm.CurrentGun.recoilScale;
			this.recoilRotation += recoilRotation;
			recoilTransform.localEulerAngles = this.recoilRotation;
			RaycastHit raycastHit;
			Ray ray = new Ray (recoilTransform.position, 
				recoilTransform.forward + UnityRandom.insideUnitSphere * innacuracy); 

			if (Physics.Raycast (ray, out raycastHit, Mathf.Infinity)) {
				var part = raycastHit.collider.GetComponent<BodyPart> ();
				if (part)
					part.player.CmdTakeDamage (wm.CurrentGun.damage, part.bodyPartType, 
						gameObject, transform.position);
				else if (!raycastHit.collider.CompareTag ("Weapon")) {
					if (raycastHit.collider.GetComponent<NetworkIdentity> ())
						RpcSpawnBulletHoleWithParent (raycastHit.point, raycastHit.normal, raycastHit.collider.gameObject);
					else
						RpcSpawnBulletHole (raycastHit.point, raycastHit.normal);
				}
				Rigidbody rb = raycastHit.rigidbody;
				if (rb && rb.GetComponent<NetworkIdentity> () && !rb.isKinematic)
					rb.AddForceAtPosition (recoilTransform.forward * 30, raycastHit.point, ForceMode.Impulse);
			}
			RpcFire (this.recoilRotation, wm.CurrentGun.recoil.Direction);
		}
	}

	[Command]
	private void CmdEmptyReload () {
		if (wm.CurrentDynamicGun.ammunitionInMagazine > 0)
			return;
		CmdReload ();
	}

	[Command]
	private void CmdReload () {
		if (wm.CurrentGun.continuousReload)
			return;
		if (wm.CurrentDynamicGun.reservedAmmunition <= 0)
			return;
		if (wm.CurrentDynamicGun.ammunitionInMagazine == wm.CurrentGun.magazineCapacity)
			return;
		reloading = true;
		RpcReload ();
		StartCoroutine (DUtil.DelayedInvoke (() => {
			int ammoToReload = wm.CurrentGun.magazineCapacity - wm.CurrentDynamicGun.ammunitionInMagazine;
			if (wm.CurrentDynamicGun.reservedAmmunition < ammoToReload) {
				ammoToReload = wm.CurrentDynamicGun.reservedAmmunition;
				wm.CurrentDynamicGun.reservedAmmunition = 0;
			}
			else
				wm.CurrentDynamicGun.reservedAmmunition -= ammoToReload;
			wm.CurrentDynamicGun.ammunitionInMagazine += ammoToReload;
			reloading = false;
		}, wm.CurrentGun.reloadDuration));
	}

	[Server]
	private void RecoilCooldown () {
		recoilRotation = DUtil.ExponentialDecayTowards (recoilRotation, Vector3.zero, 1f, Time.deltaTime * 5f);
		recoilTransform.localRotation = Quaternion.Euler (recoilRotation);
		view.recoilTrackingRotation = recoilRotation;

		if (Time.time >= nextRecoilCooldownTime) {
			innacuracy = Mathf.MoveTowards (innacuracy, wm.CurrentGun.baseInnacuracy, Time.deltaTime * 2);
			wm.CurrentGun.recoil.Reset ();
		}
	}

	[ClientRpc]
	private void RpcReload () {
		
	}

	[ClientRpc]
	private void RpcFire (Vector3 recoilRotation, Vector3 recoilDirection) {
		view.recoilTrackingRotation = recoilRotation;
		view.PunchDirection = recoilDirection;
	}

	[ClientRpc]
	private void RpcSpawnBulletHoleWithParent (Vector3 position, Vector3 direction, GameObject parent) {
		Destroy (Instantiate (bulletHolePrefab, position, Quaternion.LookRotation (direction), parent.transform), 20);
	}
	// rpc spawn bullet hole
	[ClientRpc]
	private void RpcSpawnBulletHole (Vector3 position, Vector3 direction) {
		Destroy (Instantiate (bulletHolePrefab, position, Quaternion.LookRotation (direction)), 20);
	}

}