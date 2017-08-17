using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using DUtil = Doxel.Utility;
using Doxel.Utility.ExtensionMethods;

public class Player : NetworkBehaviour {
	
	public static Action<string> DeathNote;
	public static Action<string> SendChatMessage;
	public event Action<int> OnHealthChanged;
	public event Action<GameObject> Die;
	public event Action<bool> Flash;

	[SyncVar (hook = "HealthChanged")]
	public int health = 100;
	[SyncVar]
	public int kills = 0;
	[SyncVar]
	public int deaths = 0;

	public Transform model;
	public GameObject ragdollPrefab;
	private GameObject ragdollInstance;
	private bool isDead = false;
	private Camera radarCam;

	public float testValue = 10;
	public float time = 0;

	private WeaponManager weaponManager;

	private void Start () {
		name = "Player " + netId;
		weaponManager = GetComponentInChildren<WeaponManager> ();

		if (isLocalPlayer) {
			// if the player is the local player, set the player model to a layermask
			// that cannot be seen by the local camera, so players cannot see themselves
			// but other players can
			foreach (var child in model.GetComponentsInChildren<Transform> ()) {
				child.gameObject.layer = LayerMask.NameToLayer ("Local Player Model");
			}
			PlayerHUD.Instance.Player = this;
			// UI feeback when a player joins
			CmdSendChat ("[Server]: " + name + " has joined the game");
			//PlayerHUD.Instance.SendChat (name + " has joined the game");
			radarCam = gameObject.GetGameObjectInChildren ("Radar Camera").GetComponent<Camera> ();
			PlayerHUD.Instance.SetRadarCam (radarCam);
		}
		else {
			enabled = false;
			GetComponent<PlayerController> ().enabled = false;
			GetComponentInChildren<Camera> ().enabled = false;
			GetComponentInChildren<AudioListener> ().enabled = false;
		}

		// UI feeback when a player joins
		PlayerHUD.Instance.AddPlayerToScoreboard (netId, name, 0, 0);
	
	}

	private void Update () {
		
		//testValue = 2 * Mathf.Exp (-time / Mathf.Log10 ((float) Math.E * 5));
		testValue = DUtil.Utility.Decay (testValue, 0, 1, Time.deltaTime);
		//testValue /= 2 * time;
		//if (testValue > 0)
		//	Debug.Log (testValue);

		if (Input.GetKeyDown (KeyCode.Z))
			CmdTakeDamage (20, BodyPartType.UpperTorso, gameObject, transform.position + Vector3.left * 10 + Vector3.forward * 10);
	}

	[Command]
	public void CmdPushRigidBody (GameObject go, Vector3 force, Vector3 point) {
		// TODO compute prediction, for now all physics simulations are done
		// in the server then relayed to the clients, this causes choppiness and
		// unsatisfactory physics. Try to do simulation on client for better results,
		// but still have the server keep track of the positions and rotations.
		go.GetComponent<Rigidbody> ().AddForceAtPosition (force, point, ForceMode.Impulse); 
		//RpcPushRigidBody (go, force, point);
	}

	[ClientRpc]
	private void RpcPushRigidBody (GameObject go, Vector3 force, Vector3 point) {
		// this function is not called for now
		go.GetComponent<Rigidbody> ().AddForceAtPosition (force, point, ForceMode.Impulse); 
	}

	private void HealthChanged (int health) {
		if (OnHealthChanged != null)
			OnHealthChanged (health);
	}

	[Command]
	public void CmdTakeDamage (int damage, BodyPartType bodyPartType, GameObject damager, Vector3 damagerPos) {
		switch (bodyPartType) {
		case BodyPartType.Head:
			damage *= 4;
			break;
		case BodyPartType.UpperTorso:
			damage *= 1;
			break;
		case BodyPartType.LowerTorso:
			damage *= 2;
			break;
		case BodyPartType.Legs:
			damage /= 2;
			break;
		}
		//print ("damaged : " + bodyPartType.ToString ());
		health -= damage;
		if (health <= 0)
			CmdDie (damager, bodyPartType);
		RpcTakeDamage (damage, bodyPartType, damager, damagerPos);
	}

	[ClientRpc]
	public void RpcTakeDamage (int damage, BodyPartType bodyPartType, GameObject damager, Vector3 damagerPos) {
		// TODO damage effects, blood ,body twitch force
		print (gameObject.name + " health is " + health);


		// FIXME fix this damage indicator, for reasons it is not
		// pointing to the source of damage
		//print (Vector3.SignedAngle (transform.forward, damagerPos - transform.position, Vector3.up));
		if (isLocalPlayer) {
			Vector3 dir = (damagerPos - transform.position).normalized;
			//dir = transform.TransformDirection (dir);
			float angle = 90 - (Mathf.Atan2 (dir.z, dir.x) * Mathf.Rad2Deg);
			print (angle);

			PlayerHUD.Instance.damageIndicator.transform.rotation = 
			Quaternion.Euler (Vector3.forward *
				-Vector3.SignedAngle (transform.forward, damagerPos - transform.position, Vector3.up));// Quaternion.Euler (Vector3.forward * angle);
		}
	}

	[Command]
	public void CmdSendChat (string msg) {
		RpcSendChat (msg);
	}

	[ClientRpc]
	private void RpcSendChat (string msg) {
		SendChatMessage (msg);
	}

	[Command]
	public void CmdSwitch (int num) {
		RpcSwitch (num);
	}

	[ClientRpc]
	public void RpcSwitch (int num) {
		weaponManager.ClientSwitch (num);

	}

	[Command]
	public void CmdScrollSwitch (int direction) {
		RpcScrollSwitch (direction);
	}

	[ClientRpc]
	public void RpcScrollSwitch (int direction) {
		weaponManager.ClientScrollSwitch (direction);
	}

	[Command]
	private void CmdOnMurder () {
		kills++;
		RpcOnMurder ();
	}

	[ClientRpc]
	private void RpcOnMurder () {
		PlayerHUD.Instance.UpdatePlayerScoreUI (netId, name, kills, deaths);
	}

	[Command]
	private void CmdDie (GameObject murderer, BodyPartType bdt) {
		if (isDead)
			return;
		isDead = true;
		murderer.GetComponent<Player> ().CmdOnMurder ();

		deaths++;
		CmdDropAllWeapons ();
		RpcDie (murderer, bdt);
	}

	[ClientRpc]
	private void RpcDie (GameObject murderer, BodyPartType bdt) {
//		if (bdt == BodyPartType.Head)
//			DeathNote (murderer.name + " Head Shotted " + gameObject.name);
//		else
//			DeathNote (murderer.name + " Killed " + gameObject.name);
//
		PlayerHUD.Instance.UpdateKillFeedList (bdt == BodyPartType.Head ?
			murderer.name + " Head Shotted " + gameObject.name :
			murderer.name + " Killed " + gameObject.name);

		PlayerHUD.Instance.UpdatePlayerScoreUI (netId, name, kills, deaths);
		
		if (Die != null)
			Die (murderer);
		// TODO Death animation, activate ragdoll and add force to damaged bodypart
		// removes the model gameobject from player hierarchy
		// then disable player GO
		//model.transform.parent = null;
		//model.GetComponent<Rigidbody> ().isKinematic = false;
		ragdollInstance = Instantiate (ragdollPrefab, model.position, model.rotation);

		//Destroy (gameObject);
		gameObject.SetActive (false);

	}

	[Command]
	public void CmdRespawn () {
		// TODO: this respawn function should me in the game manager class
		// it dosent make sense to have player respawn itself,
		// also it would be more convenient because the heiarchy is
		// messed up when the player dies, when respawning, a new player
		// prefab should be instantiated

		//(GameManager.singleton as GameManager).Respawn (gameObject);
		health = 100;
		isDead = false;
		RpcRespawn ();
	}

	[ClientRpc]
	private void RpcRespawn () {
		// TODO respawn animation
		Destroy (ragdollInstance, 1);

		transform.position = NetworkManager.singleton.GetStartPosition ().position;
		gameObject.SetActive (true);
	}

	[ClientRpc]
	public void RpcSyncBullet (GameObject bullet, Vector3 position, Quaternion rot, GameObject parent) {
		bullet.transform.SetParent (parent.transform);
		bullet.transform.localPosition = position;
		bullet.transform.localRotation = rot;
	}

	[Command]
	public void CmdSpawn (GameObject gameObject, GameObject parent) {
		NetworkServer.Spawn (gameObject);
		gameObject.transform.SetParent (parent.transform);
		RpcSpawn (gameObject, parent);
	}

	[ClientRpc]
	private void RpcSpawn (GameObject gameObject, GameObject parent) {
		gameObject.transform.SetParent (parent.transform);
	}

	[Command]
	public void CmdDropWeapon (int index) {
		weaponManager.ServerDropWeapon (index);
	}

	[Command]
	public void CmdDropAllWeapons () {
		weaponManager.ServerDropAllWeapons ();
	}

	[ClientRpc]
	public void RpcDropWeapon (int index) {
		// DO drop weapon animation
		weaponManager.ClientDeleteWeapon (index);
	}

	[Command]
	public void CmdPickupWeapon (GameObject weapon) {
		//RpcPickupWeapon (weapon);
		weaponManager.ServerPickupWeapon (weapon);
	}

	[ClientRpc]
	public void RpcPickupWeapon (GameObject weapon) {
		// Do pickup weapon animation
		weaponManager.ClientPickupWeapon (weapon);
	}

	[Command]
	public void CmdDespawn (GameObject gm) {
		NetworkServer.Destroy (gm);
	}

	[Command]
	public void CmdThrow (float strength) {
		(weaponManager.HoldingWeapon as Grenade).ServerThrow (strength);

	}

	[ClientRpc]
	public void RpcDeleteWeapon (int index) {
		weaponManager.ClientDeleteWeapon (index);
	}

	[Command]
	public void CmdDeploy () {
		(weaponManager.HoldingWeapon as Gun).ServerDeploy ();
	}

	[Command]
	public void CmdFire () {
		(weaponManager.HoldingWeapon as Gun).ServerTryFire ();
	}

	[ClientRpc]
	public void RpcFire (int ammoInMag, Vector2 v, float r, Vector2 rd) {
		(weaponManager.HoldingWeapon as Gun).ClientFire (ammoInMag, v, r, rd);
	}

	[Command]
	public void CmdReload () {
		(weaponManager.HoldingWeapon as Gun).ServerReload ();
	}

	[Command]
	public void CmdContReload () {
		// HACK: just to remove the null ref exception, might cause other
		// unwanted side effects down the road
		if (weaponManager.HoldingWeapon != null)
			(weaponManager.HoldingWeapon as Gun).ServerContReload ();
	}

	[Command]
	public void CmdEmptyReload () {
		// HACK: just to remove the null ref exception, might cause other
		// unwanted side effects down the road
		if (weaponManager.HoldingWeapon != null)
			(weaponManager.HoldingWeapon as Gun).ServerEmptyReload ();
	}

	[ClientRpc]
	public void RpcStartReload () {
		(weaponManager.HoldingWeapon as Gun).ClientStartReload ();
	}

	[ClientRpc]
	public void RpcEndReload (int ammoInMag, int reserve) {
		(weaponManager.HoldingWeapon as Gun).ClientEndReload (ammoInMag, reserve);
	}

	[Command]
	public void CmdSetScopeState (int value) {
		(weaponManager.HoldingWeapon as Gun).ServerSetScopeState (value);
	}

	[Command]
	public void CmdCycleScopeState () {
		(weaponManager.HoldingWeapon as Gun).ServerCycleScopeState ();
	}

	[ClientRpc]
	public void RpcSetScopeState (float newFOV, Vector2 newSense, bool scopeActive) {
		(weaponManager.HoldingWeapon as Gun).ClientSetScopeState (newFOV, newSense, scopeActive);
	}

	public void Flashed (bool direct) {
		// NOTE: method not called anywhere atm

		// play flashed animation
		if (Flash != null)
			Flash (direct);
	}

	[Command]
	public void CmdSwing () {
		(weaponManager.HoldingWeapon as Knife).ServerTrySwing ();
	}

	[Command]
	public void CmdStab () {
		(weaponManager.HoldingWeapon as Knife).ServerTryStab ();
	}

	[ClientRpc]
	public void RpcSwing () {
		(weaponManager.HoldingWeapon as Knife).ClientSwing ();
	}

	[ClientRpc]
	public void RpcStab () {
		(weaponManager.HoldingWeapon as Knife).ClientStab ();
	}

}