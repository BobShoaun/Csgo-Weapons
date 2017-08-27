using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityRandom = UnityEngine.Random;
using Doxel.Utility.ExtensionMethods;
using DUtil = Doxel.Utility.Utility;

public class GunHandler : Handler {

	// client
	public View view;
	private Transform muzzle;
	public GameObject bulletTracerPrefab;
	public GameObject bulletHolePrefab;

	// server 
	public Transform recoilTransform;
	private float nextFireTime = 0;
	private float nextContinuousReloadTime = 0;
	private float nextRecoilCooldownTime = 0;
	private float nextRescopeTime = 0;
	private float endReloadTime = 0;
	private bool reloading = false;
	private float innacuracy = 0;
	private Vector3 recoilRotation;
	private Vector2 initialSense;
	private int scopeState;
	private int previousScopeState;
	private bool rescopePending = false;

	public Gun Gun { get; set; }

	[ServerCallback]
	private void Start () {
		initialSense = GetComponent<PlayerController> ().sensitivity;
	}

	protected override void ServerDeploy () {
		Gun = GetComponent<WeaponManager2> ().CurrentWeapon as Gun;
		// reset all vars
		nextFireTime = Time.time + Gun.deployDuration; // factor in deploy time
		nextContinuousReloadTime = 0;
		nextRecoilCooldownTime = 0;
		nextRescopeTime = 0;
		endReloadTime = 0;
		reloading = false;
		innacuracy = Gun.baseInnacuracy;
		scopeState = 0;
		previousScopeState = 0;
		rescopePending = false;

		RpcCrosshair (Gun.showCrosshair);
		RpcUpdateUI (Gun.ammunitionInMagazine, Gun.reservedAmmunition, Gun.Name);
	}

	protected override void ClientDeploy () {
		muzzle = GetComponent<WeaponManager2> ().HoldingWeapon.GetGameObjectInChildren ("Muzzle").transform;
	}

	protected override void ServerKeep () {
		SetScopeState (0);
	}

	protected override void ServerUpdate () {
		if (Gun.continuousReload)
			ContinuousReload ();

		recoilRotation = DUtil.ExponentialDecayTowards (recoilRotation, Vector3.zero, 1f, Time.deltaTime * 5f);
		recoilTransform.localRotation = Quaternion.Euler (recoilRotation);
		RpcRecoilCooldown (recoilRotation);

		if (Time.time >= nextRecoilCooldownTime) { // RESET RECOIL AND ACCURACY
			innacuracy = Mathf.MoveTowards (innacuracy, Gun.baseInnacuracy, Time.deltaTime * 2);
			Gun.recoil.Reset ();
		}

		if (reloading && Time.time >= endReloadTime) { // RELOAD
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
		}

		if (Gun.smartScope && rescopePending && Time.time >= nextRescopeTime) { // RESCOPING
			rescopePending = false;
			SetScopeState (previousScopeState);
		}
	}

	protected override void ClientUpdate () {
		if (!isLocalPlayer)
			return;
		if (Input.GetKeyDown (KeyCode.R))
			CmdReload ();

		if (Input.GetMouseButton (0))
			CmdFire (Input.GetMouseButtonDown (0));
		else
			CmdEmptyReload ();

		if (Input.GetMouseButtonDown (1))
			CmdCycleScopeState ();

		if (Input.GetKeyDown (KeyCode.F)) {
			// Inspect
		}
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

			if (Gun.ammunitionInMagazine % Gun.tracerBulletInterval == 0) {
				RpcSpawnTracer (ray.direction);
			}
		}

		if (Gun.smartScope) {
			SetScopeState (0);
			nextRescopeTime = nextFireTime;
			rescopePending = true;
		}
		RpcFire (this.recoilRotation, Gun.recoil.Direction);
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
		if (reloading)
			return;
		SetScopeState (0); // Unscope when reloading
		if (Gun.smartScope) { // If scope is smart then prepare for rescope
			nextRescopeTime = Time.time + Gun.reloadDuration;
			rescopePending = true;
		}
		reloading = true;
		endReloadTime = Time.time + Gun.reloadDuration;
		RpcReload ();
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


	[Command]
	private void CmdCycleScopeState () {
		if (Gun.scope == Gun.Scope.None)
			return;
		if (Time.time < nextFireTime)
			return;
		SetScopeState (DUtil.Remainder (scopeState + 1, Gun.scope == Gun.Scope.Generic ? 3 : 2));
	}

	[Server]
	private void SetScopeState (int value) {
		if (Gun.scope == Gun.Scope.None)
			return;
		if (reloading) // cant scope when reloading
			return;
		if (rescopePending)
			return;
		if (value > 2) { // Error checking
			Debug.LogError ("Invalid value for scope state, has to be between 0 to 2");
			return;
		}
		switch (value) {
			case 0: // unscoped
				RpcSetScopeState (60, initialSense, false);
				RpcCrosshair (Gun.showCrosshair);
				break;
			case 1: // scoped
				RpcSetScopeState (40, initialSense / 2, true);
				RpcCrosshair (false);
				break;
			case 2: // zoom
				RpcSetScopeState (20, initialSense / 4, true);
				RpcCrosshair (false);
				break;
		}
		previousScopeState = scopeState;
		scopeState = value;
	}

	[ClientRpc]
	private void RpcSetScopeState (float newFOV, Vector2 newSense, bool scoped) {
		if (!isLocalPlayer)
			return;
		view.FieldOfView = newFOV;
		GetComponent<PlayerController> ().sensitivity = newSense;
		PlayerHUD.Instance.scopeOverlay.SetActive (scoped);
	}

	[ClientRpc]
	private void RpcRecoilCooldown (Vector3 recoilRotation) {
		view.recoilTrackingRotation = recoilRotation;
	}

	[ClientRpc]
	private void RpcReload () {
		// TODO reload animation
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

	[ClientRpc]
	private void RpcSpawnTracer (Vector3 direction) {
		Instantiate (bulletTracerPrefab, muzzle.position, Quaternion.LookRotation (direction));
	}


}