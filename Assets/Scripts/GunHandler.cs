using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityRandom = UnityEngine.Random;
using Doxel.Utility.ExtensionMethods;
using DUtil = Doxel.Utility.Utility;
using System;
using System.Linq;
using DMat = Doxel.Environment.Material;

public class GunHandler : Handler {

	// Client
	public View view;
	private Transform thirdPersonMuzzle;
	private Transform firstPersonMuzzle;
	public GameObject bulletTracerPrefab;
	public GameObject bulletHolePrefab;
	public GameObject bloodSplatterPrefab;
	private AudioSource audioSource;

	// Both
	private Gun gun;

	// Server
	[SerializeField]
	private LayerMask shootableLayer;
	private Aim aim;
	private float nextFireTime = 0;
	private float nextContinuousReloadTime = 0;
	private float nextRecoilCooldownTime = 0;
	private float nextRescopeTime = 0;
	private float endFireReadyReloadTime = 0;
	private float endClipReadyReloadTime = 0;
	private bool reloading = false;
	private float innacuracy = 0;
	private Vector3 recoilRotation;
	private Vector2 initialSense;
	private int scopeState;
	private int previousScopeState;
	private bool rescopePending = false;

	protected override Type WeaponType {
		get { return typeof (Gun); }
	}

	private void Start () {
		if (isServer) {
			aim = GetComponent<Aim> ();
			initialSense = GetComponent<PlayerController> ().sensitivity;
		}
		if (isClient)
			audioSource = GetComponent<AudioSource> ();
	}

	protected override void ServerDeploy (Weapon weapon) {
		base.ServerDeploy (weapon);
		gun = weapon as Gun;

		nextFireTime = Time.time + gun.deployDuration; // factor in deploy time
		nextContinuousReloadTime = 0;
		nextRecoilCooldownTime = 0;
		nextRescopeTime = 0;
		endFireReadyReloadTime = 0;
		reloading = false;
		innacuracy = gun.baseInnacuracy;
		scopeState = 0;
		previousScopeState = 0;
		rescopePending = false;

		RpcUpdateAmmo (gun.ammunitionInMagazine);
		RpcUpdateReservedAmmo (gun.reservedAmmunition);
	}

	protected override void ClientDeploy (Weapon weapon) {
		base.ClientDeploy (weapon);
		gun = weapon as Gun;
		if (isLocalPlayer)
			firstPersonMuzzle = firstPersonViewmodel.GetGameObjectInChildren ("Muzzle").transform;
		else
			thirdPersonMuzzle = thirdPersonWeaponModel.GetGameObjectInChildren ("Muzzle").transform;
	}

	protected override void ServerKeep () {
		print ("GUN KEEP");
		SetScopeState (0);
	}

	protected override void ServerUpdate () {
		if (gun.continuousReload)
			ContinuousReload ();

		//recoilRotation = DUtil.ExponentialDecayTowards (recoilRotation, Vector3.zero, 1f, Time.deltaTime * 5f);
		//aim.localRotation = Quaternion.Euler (recoilRotation);
		//RpcRecoilCooldown (recoilRotation);

		if (Time.time >= nextRecoilCooldownTime) { // RESET RECOIL AND ACCURACY
			innacuracy = Mathf.MoveTowards (innacuracy, gun.baseInnacuracy, Time.deltaTime * 2);
			gun.recoil.Reset ();
		}

		if (reloading) {
			if (Time.time >= endClipReadyReloadTime) { // RELOAD CLIP
				int ammoToReload = gun.magazineCapacity - gun.ammunitionInMagazine;
				if (gun.reservedAmmunition < ammoToReload) {
					ammoToReload = gun.reservedAmmunition;
					gun.reservedAmmunition = 0;
				}
				else
					gun.reservedAmmunition -= ammoToReload;
				gun.ammunitionInMagazine += ammoToReload;
				RpcUpdateAmmo (gun.ammunitionInMagazine);
				RpcUpdateReservedAmmo (gun.reservedAmmunition);
			}
			if (Time.time >= endFireReadyReloadTime) // END reload and can fire
				reloading = false;
		}

		if (gun.smartScope && rescopePending && Time.time >= nextRescopeTime) { // RESCOPING
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
			// TODO Inspect
		}
	}

	[Command]
	private void CmdFire (bool mouseDown) {
		if (!mouseDown && !gun.continuousFire)
			return;
		if (reloading) //if reloding then return out
			return;
		if (Time.time < nextFireTime)
			return;
		if (gun.ammunitionInMagazine <= 0) // if no more ammo then return out
			return;
		nextFireTime = Time.time + 1 / gun.fireRate;
		gun.ammunitionInMagazine--;
		// The first reload after a shot takes the longest
		if (gun.continuousReload)
			nextContinuousReloadTime = Time.time + gun.fireReadyReloadDuration * 3; 
		//gun.recoil.MoveNext ();

		for (int i = 0; i < gun.bulletsPerShot; i++) {
			// TODO get a better formula that factors in movement innacuracy, 
			// for now it is just adding it linearly
			innacuracy += gun.accuracyDecay;
			nextRecoilCooldownTime = Time.time + gun.recoilCooldown;
			//aim.localEulerAngles += recoilRotation;
			aim.AddRotation (gun.recoil.Rotation);
			//aim.localEulerAngles = this.recoilRotation;
			//RaycastHit raycastHit;
			//create ray with recoil and innacuracy applied
			int damage = gun.damage; // dynamic damage, initialized with every shot with base damage, but will be changed by penetration
			bool hitPlayer = false;
			var shotPlayers = new List<NetworkInstanceId> ();
			var ray = new Ray (aim.Origin, 
				//aim.Direction + UnityRandom.insideUnitSphere * innacuracy); 
			aim.Direction); 

			RaycastHit [] raycastHits = Physics.RaycastAll (ray, gun.range, shootableLayer).OrderBy (raycastHit => raycastHit.distance).ToArray ();
			foreach (var raycastHit in raycastHits) {
				DMat mat;
				BodyPart bodyPart;
				if (bodyPart = raycastHit.collider.GetComponent<BodyPart> ()) {
					if (netId == bodyPart.NetId) {
						// Hit itself
						continue;
					}
					else {
						if (shotPlayers.Contains (bodyPart.NetId)) // shot the same player mroe than once, hit hand, then torso, then hand again
							continue;
						shotPlayers.Add (bodyPart.NetId);
						bodyPart.TakeDamage (damage, gameObject, transform.position);
						hitPlayer = true;
						// DONE blood splatter behind
					}
				}
				else if (mat = raycastHit.collider.GetComponent<DMat> ()) {
					Ray reverseRay = new Ray (ray.GetPoint (gun.range), -ray.direction);
					RaycastHit reverseRaycastHit;
					if (raycastHit.collider.Raycast (reverseRay, out reverseRaycastHit, gun.range - raycastHit.distance)) {
						float thickness = Vector3.Distance (raycastHit.point, reverseRaycastHit.point);
						damage -= (int) (thickness / gun.penetrationPower * mat.penetrationPrevention);
						// TODO spawn specific bullet hole for different materials
						SpawnBulletHole (reverseRaycastHit.point, reverseRaycastHit.normal, reverseRaycastHit.collider.gameObject);
						print ("HIT WALL " + raycastHit.collider.name + " with thickness " + thickness + " Damage reduced from " + gun.damage + " to " + damage);
					}
					SpawnBulletHole (raycastHit.point, raycastHit.normal, raycastHit.collider.gameObject);
				}
				else if (!raycastHit.collider.CompareTag ("Weapon")) {
					Ray reverseRay = new Ray (ray.GetPoint (gun.range), -ray.direction);
					RaycastHit reverseRaycastHit;
					if (raycastHit.collider.Raycast (reverseRay, out reverseRaycastHit, gun.range - raycastHit.distance)) {
						float thickness = Vector3.Distance (raycastHit.point, reverseRaycastHit.point);
						damage -= (int) (thickness / gun.penetrationPower * 5f);
						SpawnBulletHole (reverseRaycastHit.point, reverseRaycastHit.normal, reverseRaycastHit.collider.gameObject);
						print ("HIT WALL " + raycastHit.collider.name + " with thickness " + thickness + " Damage reduced from " + gun.damage + " to " + damage);
					}
					SpawnBulletHole (raycastHit.point, raycastHit.normal, raycastHit.collider.gameObject);
					if (hitPlayer) {
						hitPlayer = false;
						RpcSpawnBloodSplatter (raycastHit.point, raycastHit.normal);
					}
				}
				Rigidbody rigidbody = raycastHit.rigidbody;
				if (rigidbody && rigidbody.GetComponent<NetworkIdentity> ())
					rigidbody.AddForceAtPosition (ray.direction * 30, raycastHit.point, ForceMode.Impulse);

			}
			if (gun.ammunitionInMagazine % gun.tracerBulletInterval == 0) {
				RpcSpawnTracer (ray.direction);
			}
		}

		if (gun.smartScope) {
			SetScopeState (0);
			nextRescopeTime = nextFireTime;
			rescopePending = true;
		}
		//RpcFire (gun.recoil.Direction);
		RpcUpdateAmmo (gun.ammunitionInMagazine);
	}

	[Server]
	private void SpawnBulletHole (Vector3 position, Vector3 direction, GameObject gameObject) {
		if (gameObject.GetComponent<NetworkIdentity> ())
			RpcSpawnBulletHoleWithParent (position, direction, gameObject);
		else
			RpcSpawnBulletHole (position, direction);
	}

	[Command]
	private void CmdEmptyReload () {
		if (!enabled)
			return;
		if (gun.ammunitionInMagazine > 0) // magazine is not empty, dont reload
			return;
		CmdReload ();
	}

	[Command]
	private void CmdReload () {
		// normal reload not applicable to those with cont
		// reload, eg shotguns
		if (gun.continuousReload)
			return;
		if (gun.reservedAmmunition <= 0) // no more reserved ammo, cant reload
			return;
		if (gun.ammunitionInMagazine == gun.magazineCapacity) // dont have to reload if mag is still full
			return;
		if (reloading)
			return;
		SetScopeState (0); // Unscope when reloading
		if (gun.smartScope) { // If scope is smart then prepare for rescope
			nextRescopeTime = Time.time + gun.fireReadyReloadDuration;
			rescopePending = true;
		}
		reloading = true;
		endClipReadyReloadTime = Time.time + gun.clipReadyReloadDuration;
		endFireReadyReloadTime = Time.time + gun.fireReadyReloadDuration;
		RpcReload ();
	}

	[Server]
	private void ContinuousReload () {
		if (!gun.continuousReload) // if not cont reload then cant use this method to reload
			return;
		if (gun.reservedAmmunition <= 0)
			return;
		if (gun.ammunitionInMagazine == gun.magazineCapacity)
			return;
		if (Time.time < nextContinuousReloadTime)
			return;
		nextContinuousReloadTime = Time.time + gun.fireReadyReloadDuration;
		gun.reservedAmmunition--;
		gun.ammunitionInMagazine++;
		RpcUpdateAmmo (gun.ammunitionInMagazine);
		RpcUpdateReservedAmmo (gun.reservedAmmunition);
	}

	[Command]
	private void CmdCycleScopeState () {
		if (gun.scope == Gun.Scope.None)
			return;
		if (Time.time < nextFireTime)
			return;
		SetScopeState (DUtil.Remainder (scopeState + 1, gun.scope == Gun.Scope.Generic ? 3 : 2));
	}

	[Server]
	private void SetScopeState (int value) {
		if (gun.scope == Gun.Scope.None)
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
				RpcSetScopeState (90, initialSense, false);
				RpcUpdateCrosshair (gun.showCrosshair);
				break;
			case 1: // scoped
				RpcSetScopeState (40, initialSense / 2, true);
				RpcUpdateCrosshair (false);
				break;
			case 2: // zoom
				RpcSetScopeState (15, initialSense / 4, true);
				RpcUpdateCrosshair (false);
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
		if (firstPersonViewmodel)
			firstPersonViewmodel.SetActive (!scoped);
	}

	[ClientRpc]
	private void RpcRecoilCooldown (Vector3 recoilRotation) {
		view.recoilTrackingRotation = recoilRotation;
	}

	[ClientRpc]
	private void RpcReload () {
		// TODO reload animation
		if (audioSource) {
			audioSource.clip = gun.reload;
			audioSource.Play ();
		}
	}

	[ClientRpc]
	private void RpcFire (Vector3 recoilDirection) {
		if (audioSource)
			audioSource.PlayOneShot (gun.shoot);
		if (!isLocalPlayer)
			return;
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
		if (isLocalPlayer) {
			if (firstPersonMuzzle == null)
				print ("MUZZLE IS NULL");
			Instantiate (bulletTracerPrefab, firstPersonMuzzle.position, Quaternion.LookRotation (direction));
		}
		else
			Instantiate (bulletTracerPrefab, thirdPersonMuzzle.position, Quaternion.LookRotation (direction));
	}

	[ClientRpc]
	private void RpcSpawnBloodSplatter (Vector3 position, Vector3 direction) {
		Destroy (Instantiate (bloodSplatterPrefab, position, Quaternion.LookRotation (direction)), 20);
	}

}