using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityRandom = UnityEngine.Random;
using DUtil = Doxel.Utility.Utility;

public class GunHandler : Handler {

	// client
	public View view;
	public Transform muzzle;
	public GameObject bulletHolePrefab;

	// server 
	public Transform recoilTransform;
	private float nextFireTime = 0;
	private float nextContinuousReloadTime = 0;
	private float nextRecoilCooldownTime = 0;
	private float nextRescopeTime = 0;
	private bool reloading = false;
	private float innacuracy = 0;
	private Vector3 recoilRotation;

	public Gun Gun { get; set; }

	[ServerCallback]
	private void OnEnable () {
		Gun = GetComponent<WeaponManager2> ().CurrentWeapon as Gun;
		RpcUpdateUI (Gun.ammunitionInMagazine, Gun.reservedAmmunition, Gun.Name);
	}

	private void Update () {
		if (isServer) {
			RecoilCooldown ();
			if (Gun.continuousReload)
				ContinuousReload ();
		}
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
		if (!mouseDown && !Gun.continuousFire)
			return;
		if (reloading) //if reloding then return out
			return;
		if (Time.time < nextFireTime)
			return;
		if (Gun.ammunitionInMagazine <= 0) // if no more ammo then return out
			return;
		nextFireTime = Time.time + 1 / Gun.fireRate;
		Gun.ammunitionInMagazine--;
		// The first reload after a shot takes the longest
		nextContinuousReloadTime = Time.time + Gun.reloadDuration * 3; 
		if (!Gun.recoil.MoveNext ())
			Gun.recoil.Reset ();

		for (int i = 0; i < Gun.bulletsPerShot; i++) {
			// TODO get a better formula that factors in movement innacuracy, 
			// for now it is just adding it linearly
			innacuracy += Gun.accuracyDecay;
			nextRecoilCooldownTime = Time.time + Gun.recoilCooldown;
			var recoilRotation = new Vector3 (-Gun.recoil.Current.y,
				Gun.recoil.Current.x) * Gun.recoilScale;
			this.recoilRotation += recoilRotation;
			recoilTransform.localEulerAngles = this.recoilRotation;
			RaycastHit raycastHit;
			//create ray with recoil and innacuracy applied
			Ray ray = new Ray (recoilTransform.position, 
				recoilTransform.forward + UnityRandom.insideUnitSphere * innacuracy); 

			if (Physics.Raycast (ray, out raycastHit, Mathf.Infinity)) {
				var part = raycastHit.collider.GetComponent<BodyPart> ();
				if (part)
					part.player.CmdTakeDamage (Gun.damage, part.bodyPartType, 
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
			RpcFire (this.recoilRotation, Gun.recoil.Direction);
		}
		RpcUpdateUI (Gun.ammunitionInMagazine, Gun.reservedAmmunition, Gun.Name);
	}

	[Command]
	private void CmdEmptyReload () {
		if (Gun.ammunitionInMagazine > 0) // magazine is not empty, dont reload
			return;
		CmdReload ();
	}

	[Command]
	private void CmdReload () {
		// normal reload not applicable to those with cont
		// reload, eg shotguns
		if (Gun.continuousReload)
			return;
		if (Gun.reservedAmmunition <= 0) // no more reserved ammo, cant reload
			return;
		if (Gun.ammunitionInMagazine == Gun.magazineCapacity) // dont have to reload if mag is still full
			return;
		reloading = true;
		RpcReload ();
		StartCoroutine (DUtil.DelayedInvoke (() => {
			int ammoToReload = Gun.magazineCapacity - Gun.ammunitionInMagazine;
			if (Gun.reservedAmmunition < ammoToReload) {
				ammoToReload = Gun.reservedAmmunition;
				Gun.reservedAmmunition = 0;
			}
			else
				Gun.reservedAmmunition -= ammoToReload;
			Gun.ammunitionInMagazine += ammoToReload;
			reloading = false;
			RpcUpdateUI (Gun.ammunitionInMagazine, Gun.reservedAmmunition, Gun.Name);
		}, Gun.reloadDuration));
	}

	[Server]
	private void ContinuousReload () {
		if (!Gun.continuousReload) // if not cont reload then cant use this method to reload
			return;
		if (Gun.reservedAmmunition <= 0)
			return;
		if (Gun.ammunitionInMagazine == Gun.magazineCapacity)
			return;
		if (Time.time < nextContinuousReloadTime)
			return;
		nextContinuousReloadTime = Time.time + Gun.reloadDuration;
		Gun.reservedAmmunition--;
		Gun.ammunitionInMagazine++;
		RpcUpdateUI (Gun.ammunitionInMagazine, Gun.reservedAmmunition, Gun.Name);
	}

	[Server]
	private void RecoilCooldown () {
		recoilRotation = DUtil.ExponentialDecayTowards (recoilRotation, Vector3.zero, 1f, Time.deltaTime * 5f);
		recoilTransform.localRotation = Quaternion.Euler (recoilRotation);
		RpcRecoilCooldown (recoilRotation);

		if (Time.time >= nextRecoilCooldownTime) {
			innacuracy = Mathf.MoveTowards (innacuracy, Gun.baseInnacuracy, Time.deltaTime * 2);
			Gun.recoil.Reset ();
		}
	}

	[ClientRpc]
	private void RpcRecoilCooldown (Vector3 recoilRotation) {
		view.recoilTrackingRotation = recoilRotation;
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