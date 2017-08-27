using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Networking;
using UnityRandom = UnityEngine.Random;
using DUtil = Doxel.Utility;
using Doxel.Utility.ExtensionMethods;

public class GunLegacy : HeldWeapon {

	[SerializeField]
	protected float fireRate = 10;
	[SerializeField]
	protected int magazineCapacity;
	[SerializeField]
	private int reservedAmmunition;
	public GameObject bulletHolePrefab;
	[SerializeField]
	protected Recoil recoil;
	protected float nextFireTime;
	[SerializeField]
	private bool continuousFire = false;
	[SerializeField]
	protected int damage = 20;
	[SerializeField]
	protected float accuracyDecay = 0.05f;
	[SerializeField]
	protected float baseInnacuracy = 0;
	protected float innacuracy = 0;
	//private float accuracyRecovery = 0.5f;
	//private float accuracyTimer;
	protected bool reloading;
	[SerializeField]
	private int bulletsPerShot = 1;

	[SerializeField]
	protected float recoilScale = 75;
	[SerializeField]
	protected float recoilCooldown = 0.55f;
	protected float nextRecoilCooldownTime = 0;
	[SerializeField]
	private float rotOffset2ReturnDuration = 0.2f;

	[SerializeField]
	protected float reloadDuration = 2.5f;
	public GameObject bulletTracerPrefab;
	public Transform muzzle;

	[SerializeField] [Range (0, 0.5f)]
	protected float viewPunchUpDuration = 0.4f;
	[SerializeField] [Range (0, 0.5f)]
	protected float viewPunchDownDuration = 0.5f;

	[SerializeField]
	private Vector2 viewPunchMagnitude = Vector2.one;
	[SerializeField]
	private bool continuousReload = false;
	private bool reloadInterrupted = false;
	private float nextContinuousReloadTime;

	[SerializeField]
	private Scope scope = Scope.None;
	[SerializeField]
	private bool smartScope = false;
	private Vector2 initialPSense;

	enum Scope { None, Generic, Unique } 

	protected float nextBulletTracerTime = 0;

	protected Player player;
	protected PlayerController pc;
	private CharacterController cc;
	private Transform recoilTransform;
	private View view;

	private int scopeState;
	private int previousScopeState;
	[SerializeField]
	private Renderer reloadIndicator;

	public event Action Fire;

	Vector2 vel;
	float timeTillRescopes;

	public int AmmunitionInMagazine { get; set; }

	public int ReservedAmmunition {
		get { return reservedAmmunition; } 
		set { reservedAmmunition = value; }
	}

	protected override void OnEnable () {
		base.OnEnable ();
		//if (hasScope)
		//	PlayerHUD.Instance.crossHair.SetActive (false);

		// TODO the scope system is super buggy, pls fix!
	}

	protected override void OnDisable () {
		
		if (!player.isClient)
			return;
		if (scope != Scope.None) {
			//PlayerHUD.Instance.crossHair.SetActive (true);
			//player.CmdSetScopeState (0);
			// FIXME: find better way to disable the scope when the weapon
			// is destroyed, this current solution may cause some bugs
			previousScopeState = 0;
			ServerSetScopeState (0); // HACK: this is bad. it is not updated on the server
		}
		if (GetComponent<Renderer> ()) {
			GetComponent<Renderer> ().material.color = Color.black;
		}
		base.OnDisable ();
	}

	void Start () {
		player = GetComponentInParent<Player> ();
		cc = GetComponentInParent<CharacterController> ();
		pc = GetComponentInParent<PlayerController> ();
		view = GetComponentInParent<View> ();
		// Find GO called "recoil" in parent TODO make an Utility method for
		// recursively finding go with name in parent or children
		recoilTransform = gameObject.GetGameObjectInParent ("Look").GetGameObjectInChildren ("Recoil").transform;

		recoil.Initialize (magazineCapacity);

		AmmunitionInMagazine = magazineCapacity;
		innacuracy = baseInnacuracy;
		initialPSense = pc.sensitivity;

		icolor = reloadIndicator.material.color;
	}

	void Update () {

		if (player.isServer) {
			// set the cooldown straight away after the cooldown duration
			ServerFireCooldown ();
		} else 
			// ALERT: client function called instead of a command
			ClientFireCooldown ();
		
		if (!player.isLocalPlayer)
			return;

		if (continuousReload)
			player.CmdContReload ();

		//player.CmdFireCooldown ();



		if (Input.GetKeyDown (KeyCode.R))
			player.CmdReload ();

		if (Input.GetMouseButtonDown (0) || continuousFire && Input.GetMouseButton (0))
			player.CmdFire ();

		if (scope != Scope.None && Input.GetMouseButtonDown (1))
			player.CmdCycleScopeState ();

		// DONE: make player reload automaticly after the ammo is depleted
		// BUT allow the reload on when the player has let go of the mouse
		// button.
		if (Input.GetMouseButtonUp (0))
			player.CmdEmptyReload ();
	}

	public override void Deploy () {

		player.CmdDeploy ();

		//if (!player.isClient)
		//	return;

	}

	public void ServerDeploy () {
		// when the weapon is deployed
		nextFireTime = Time.time + deployTime;
		ServerEmptyReload (); // try to reload if there is not ammo
	}

	public void ServerCycleScopeState () {
		if (scope == Scope.None)
			return;
		ServerSetScopeState (DUtil.Utility.Remainder (scopeState + 1, scope == Scope.Generic ? 3 : 2));
	}

	public void ServerSetScopeState (int value) {
		if (scope == Scope.None)
			return;

		// Error checking
		if (value > 2) {
			Debug.LogError ("Invalid value for scope state, has to be between 0 to 2");
			return;
		}
		float newFOV = 0;
		Vector2 newSense = Vector2.zero;
		bool isScopeActive = false;
		switch (value) {
			case 0:
			// unscoped
				newFOV = 60;
				newSense = initialPSense;
				isScopeActive = false;
				break;
			case 1:
				if (!canScope)
					return;
			// scoped
				newFOV = 40;
				newSense = initialPSense / 2;
				isScopeActive = true;
				break;
			case 2:
				if (!canScope)
					return;
			// zoom scoped
				newFOV = 20;
				newSense = initialPSense / 4;
				isScopeActive = true;
				break;
		}
		previousScopeState = scopeState;
		scopeState = value;
		SetUpdatedScopedSettings (newFOV, newSense);
		player.RpcSetScopeState (newFOV, newSense, isScopeActive);
	}

	private void SetUpdatedScopedSettings (float newFOV, Vector2 newSense) {
		view.FieldOfView = newFOV;
		pc.sensitivity = newSense;
	}

	public void ClientSetScopeState (float newFOV, Vector2 newSense, bool scopeActive) {
		if (player.isLocalPlayer) {
			if (scope == Scope.Generic) {
				PlayerHUD.Instance.scopeOverlay.SetActive (scopeActive);
			}
			else if (scope == Scope.Unique) {
				// activate unique scope;
			}
			if (!scopeActive)
				PlayerHUD.Instance.crossHair.SetActive (showCrossHair);
		}
		SetUpdatedScopedSettings (newFOV, newSense);
	}

	private IEnumerator ColorChange () {
		if (!reloadIndicator)
			yield break;
		reloadIndicator.material.color = Color.red;
		yield return new WaitForSeconds (1f / fireRate);
		reloadIndicator.material.color = Color.black;
	}

	private IEnumerator ReloadColorChange () {
		if (!reloadIndicator)
			yield break;
		Color icolor = reloadIndicator.material.color;
		reloadIndicator.material.color = Color.yellow;
		yield return new WaitForSeconds (reloadDuration);
		reloadIndicator.material.color = icolor;
	}

	public void ServerReload () {
		// normal reload not applicable to those with cont
		// reload, eg shotguns
		if (continuousReload)
			return;
		// no more reserved ammo, cant reload
		if (reservedAmmunition <= 0)
			return;
		// dont have to reload if mag is still full
		if (AmmunitionInMagazine == magazineCapacity)
			return;

		// because if it was a smart scope, i will have already set been disabled, and this is so
		// that the previous scope state wont be replaced
		if (!smartScope)
			ServerSetScopeState (0);
		timeTillRescopes = Time.time + reloadDuration;

		reloading = true;
		player.RpcStartReload ();
		StartCoroutine (DUtil.Utility.DelayedInvoke (() => {
			int ammoToReload = magazineCapacity - AmmunitionInMagazine;
			if (reservedAmmunition < ammoToReload) {
				ammoToReload = reservedAmmunition;
				ReservedAmmunition = 0;
			}
			else
				ReservedAmmunition -= ammoToReload;
			AmmunitionInMagazine += ammoToReload;
			player.RpcEndReload (AmmunitionInMagazine, reservedAmmunition);
			reloading = false;
//
//			canScope = true;
//			if (smartScope)
//				ServerSetScopeState (previousScopeState);
//
		}, reloadDuration));
	}

	public void ServerEmptyReload () {
		// magazine is not empty, dont reload
		if (AmmunitionInMagazine > 0)
			return;
		ServerReload ();
	}

	public void ServerContReload () {
		// if not cont reload then cant use this method to reload
		if (!continuousReload)
			return;
		if (reservedAmmunition <= 0)
			return;
		if (AmmunitionInMagazine == magazineCapacity)
			return;
		if (Time.time < nextContinuousReloadTime)
			return;
		nextContinuousReloadTime = Time.time + reloadDuration;
		ReservedAmmunition--;
		AmmunitionInMagazine++;
		player.RpcEndReload (AmmunitionInMagazine, reservedAmmunition);
	}

	Color icolor;
	public void ClientEndReload (int ammoInMag, int reserve) {
		// feed back when reload is over
		if (player.isLocalPlayer) {
			PlayerHUD.Instance.WeaponAmmo = ammoInMag;
			PlayerHUD.Instance.WeaponReserve = reserve;
		}
		if (!reloadIndicator)
			return;
		reloadIndicator.material.color = icolor;
	}

	public void ClientStartReload () {
		//StartCoroutine (ReloadColorChange ());
		if (!reloadIndicator)
			return;
		
		reloadIndicator.material.color = Color.yellow;

	}

	bool calledScope = false, canScope = true;
	float lerpTime = 3;
	float currentLerpTime;
	//Vector3 previousRecoil;
	Vector3 finalRecoil;
	//bool transition = false;

	private void ServerFireCooldown () {

		//StartCoroutine (DUtil.Utility.Transition (result => finalRecoil = result, 1 / fireRate, previousRecoil, finalRecoil,
		//	() => transition = true));

		//finalRecoil = Vector2.MoveTowards (finalRecoil, Vector2.zero, Time.deltaTime * 50);
		//if (transition)
		finalRecoil = DUtil.Utility.ExponentialDecayTowards (finalRecoil, Vector2.zero, 1f, Time.deltaTime * 5f);
			//Vector3.MoveTowards (finalRecoil, Vector3.zero, Time.deltaTime * 1);
		recoilTransform.localRotation = Quaternion.Euler (finalRecoil);
		view.recoilTrackingRotation = finalRecoil;
		// Server does cool down too,  this is not the simplified version, it is
		// not implemented yet

		//pc.rotationOffset2 = Vector2.MoveTowards (pc.rotationOffset2, Vector2.zero
		//pc.rotationOffset2 = Vector2.MoveTowards (pc.rotationOffset2, Vector2.zero, Time.deltaTime * 10);
		//pc.rotationOffset = Vector2.MoveTowards (pc.rotationOffset, Vector2.zero, Time.deltaTime * 30);
		//pc.rotationOffset2 = Vector2.SmoothDamp (pc.rotationOffset2, Vector2.zero, ref vel, 
		//	vel.sqrMagnitude > 100 ? 0.3f : 0.2f, Mathf.Infinity, Time.deltaTime);

		//pc.rotationOffset2 = Vector2.SmoothDamp (pc.rotationOffset2, Vector2.zero, ref vel, 
		//	rotOffset2ReturnDuration, Mathf.Infinity, Time.deltaTime);

		//pc.rotationOffset2 = Vector2.MoveTowards (pc.rotationOffset2, Vector2.zero, Time.deltaTime * rotOffset2ReturnDuration);

		if (Time.time >= nextRecoilCooldownTime) {
			innacuracy = Mathf.MoveTowards (innacuracy, baseInnacuracy, Time.deltaTime * 2);
			recoil.Reset ();
		}
//
//		if (Time.time >= nextFireTime) {
//			if (smartScope && !calledScope) {
//				calledScope = true;
//				canScope = true;
//				ServerSetScopeState (previousScopeState);
//			}
//		}
			
		// scoping mechanism
		if (Time.time >= timeTillRescopes) {
			if (smartScope && !calledScope) {
				calledScope = true;
				canScope = true;
				ServerSetScopeState (previousScopeState);
			}
		}
		else {
			calledScope = false;
			canScope = false;
		}
		//yield return new WaitForSeconds (rotOffset2ReturnDuration * 3);
		//pc.rotationOffset2 = Vector2.zero;
	}

	public void ClientFireCooldown () {
		// On the client side, there will be a gradual cool down, this will be called
		// every frame. On the server side, the cool down will be set straight away after
		// the cooldown duration.
	//	pc.rotationOffset2 = Vector2.SmoothDamp (pc.rotationOffset2, Vector2.zero, ref vel, 
		//	rotOffset2ReturnDuration, Mathf.Infinity, Time.deltaTime);

		//pc.rotationOffset2 = Vector2.MoveTowards (pc.rotationOffset2, Vector2.zero, Time.deltaTime * rotOffset2ReturnDuration);


		// below code is still needed

//		if (Time.time >= nextRecoilCooldownTime) {
//			innacuracy = Mathf.MoveTowards (innacuracy, baseInnacuracy, Time.deltaTime * 2);
//			//recoil.Reset ();
//		}
	}

	public void ClientFire (int ammoInMag, Vector2 recoilRotation, float nextRecCool, Vector2 recoilDir) {
		// Do Shooting feedback like recoil, muzzleflash, animations
		if (player.isLocalPlayer) {
			PlayerHUD.Instance.WeaponAmmo = ammoInMag;
		}
		view.recoilTrackingRotation = recoilRotation;
		view.PunchDirection = recoilDir;

		nextRecoilCooldownTime = nextRecCool;

		// below code is totally visual, just do in client
//		StartCoroutine (DUtil.Utility.Transition (result => pc.rotationOffset = result, viewPunchUpDuration / fireRate, pc.rotationOffset, 
//			new Vector2 (-0.5f * viewPunchMagnitude.x, UnityRandom.Range (-0.25f, 0.25f) * viewPunchMagnitude.y), () => 
//			StartCoroutine (DUtil.Utility.Transition (result => pc.rotationOffset = result, viewPunchDownDuration / fireRate, pc.rotationOffset,
//			Vector2.zero))));
//
//		StartCoroutine (DUtil.Utility.Transition (result => pc.rotationOffset = result, viewPunchUpDuration / fireRate, pc.rotationOffset, 
//			new Vector2 (-recoilDir.y * viewPunchMagnitude.y, recoilDir.x * viewPunchMagnitude.x), () => 
//			StartCoroutine (DUtil.Utility.Transition (result => pc.rotationOffset = result, viewPunchDownDuration / fireRate, pc.rotationOffset,
//				Vector2.zero))));
	}

	public void ServerTryFire (LayerMask shootableLayer) {
		//if reloding then return out
		if (reloading)
			return;

		if (Time.time < nextFireTime)
			return;
		// if no more ammo then return out
		if (AmmunitionInMagazine <= 0)
			return;

		nextFireTime = Time.time + 1 / fireRate;

		if (Fire != null)
			Fire ();

		AmmunitionInMagazine--;

		nextContinuousReloadTime = Time.time + reloadDuration * 3;

		if (!recoil.MoveNext ())
			recoil.Reset ();

		for (int i = 0; i < bulletsPerShot; i++) {
			
			innacuracy += accuracyDecay;
			nextRecoilCooldownTime = Time.time + recoilCooldown;

			var recoilRotation = new Vector3 (-recoil.Current.y, recoil.Current.x) * recoilScale;
			//var recoilDirection = (recoil.Next - recoil.Current).normalized;
			// TODO get a better formula that factors in movement innacuracy, 
			// for now it is just adding it linearly
			// = finalRecoil;
			finalRecoil += recoilRotation;
			//transition = false;
			recoilTransform.localEulerAngles = finalRecoil;
			//recoilTransform.localEulerAngles = (Vector3) recoilRotation;
			//recoilTransform.localRotation = Quaternion.Lerp (recoilTransform.localRotation, Quaternion.Euler (recoilRotation), Time.deltaTime);

			RaycastHit hit;
			//create ray with recoil and innacuracy applied
			Ray ray = new Ray (recoilTransform.position, 
				recoilTransform.forward + UnityRandom.insideUnitSphere * innacuracy); 

//			(Quaternion.Euler ((Vector3) recoilRotation +
//				          UnityRandom.insideUnitSphere * (innacuracy + cc.velocity.sqrMagnitude)
//				          ) * Vector3.forward));

			if (Physics.Raycast (ray, out hit, Mathf.Infinity, shootableLayer)) {
				var part = hit.collider.GetComponent<BodyPart> ();
				if (part)
					part.player.CmdTakeDamage (damage, part.bodyPartType, 
						player.gameObject, player.gameObject.transform.position);
				else if (!hit.collider.CompareTag ("Weapon")) {
					var bulletHole = Instantiate (bulletHolePrefab, hit.point, Quaternion.LookRotation (hit.normal));
					bulletHole.transform.SetParent (hit.transform);
					if (hit.transform.GetComponent<NetworkIdentity> ())
						player.CmdSpawn (bulletHole, hit.transform.gameObject);
					else
						NetworkServer.Spawn (bulletHole);
					//					if (hit.transform.GetComponent<NetworkIdentity> ())
					//						GetComponentInParent<Player> ().RpcSyncBullet (bulletHole, bulletHole.transform.localPosition, 
					//							bulletHole.transform.localRotation, hit.transform.gameObject);
					Destroy (bulletHole, 20);
				}
			
				Rigidbody rb = hit.rigidbody;
				if (rb && rb.GetComponent<NetworkIdentity> () && !rb.isKinematic)
					rb.AddForceAtPosition (recoilTransform.forward * 30, hit.point, ForceMode.Impulse);
			}

			if (Time.time >= nextBulletTracerTime) {
				nextBulletTracerTime = Time.time + recoilCooldown;
				GameObject bulletTracer = Instantiate (bulletTracerPrefab, muzzle.position, 
					Quaternion.LookRotation (ray.direction));
				NetworkServer.Spawn (bulletTracer);
			}

			if (smartScope) {
				timeTillRescopes = nextFireTime;
				//calledScope = false;

				ServerSetScopeState (0); // after firing the scope is exited
				//canScope = false;
			}

			// mini recoil tracking for crosshair
			//pc.rotationOffset2 += recoilRotation * recoilScale / 2;

			// rotOffset2 is essentially a SyncVar
			//player.RpcSetRotationOffset (pc.rotationOffset2);
			player.RpcFire (AmmunitionInMagazine, finalRecoil, nextRecoilCooldownTime, recoil.Direction);

			// Server doing fire cooldown, a simpler version that just sets it straightaway
			//StartCoroutine (ServerFireCooldown ());
		}

	}


}
