using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using DUtil = Doxel.Utility.Utility;
using Doxel.Utility.ExtensionMethods;

public class Player : NetworkBehaviour {

	// Client
	public Transform model;
	public GameObject ragdollPrefab;
	private GameObject ragdollInstance;

	// Server
	public int health = 100;
	public int kills = 0;
	public int deaths = 0;
	private bool isDead = false;

	public float testValue = 10;
	public float time = 0;
	public LayerMask shootableLayer;

	public override void OnStartLocalPlayer () {
		base.OnStartLocalPlayer ();
	
		GetComponent<PlayerController> ().enabled = true;
		PlayerHUD.Instance.player = this;
		GameObject radarCam = gameObject.GetGameObjectInChildren ("Radar Camera", true);
		radarCam.SetActive (true);
		PlayerHUD.Instance.SetRadarCam (radarCam.GetComponent<Camera> ());
		foreach (Renderer renderer in model.GetComponentsInChildren<Renderer> ()) {
			renderer.enabled = false;
		}

		gameObject.GetGameObjectInChildren ("Environment Camera", true).SetActive (true);
	}

	[ServerCallback]
	private void Start () {
		name = "Player " + netId;
		RpcInitialize (name, health, kills, deaths);
		RpcSendChat ("[Server]: " + name + " has joined the game");
	}

	private void Update () {
		ClientUpdate ();
		ServerUpdate ();
	}

	[ClientCallback]
	private void ClientUpdate () {
		if (!isLocalPlayer)
			return;
		if (Input.GetKeyDown (KeyCode.Z))
			TakeDamage (20, gameObject, 
				transform.position + Vector3.left * 10 + Vector3.forward * 10, BodyPartType.UpperTorso);
	}

	[ServerCallback]
	private void ServerUpdate () {
		//testValue = 2 * Mathf.Exp (-time / Mathf.Log10 ((float) Math.E * 5));
		testValue = DUtil.Decay (testValue, 0, 1, Time.deltaTime);
		//testValue /= 2 * time;
		//if (testValue > 0)
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

	[Server]
	public void TakeDamage (int damage, GameObject damager, Vector3 damagerPos, BodyPartType bodyPartType) {
		if (damage <= 0)
			return;
		health -= damage;
		if (health <= 0)
			Die (damager, bodyPartType);
		RpcTakeDamage (health, damagerPos);
	}

	[ClientRpc]
	public void RpcTakeDamage (int health, Vector3 damagerPos) {
		// TODO damage effects, blood ,body twitch force

		print (gameObject.name + " health is " + health);


		// FIXME fix this damage indicator, for reasons it is not
		// pointing to the source of damage
		//print (Vector3.SignedAngle (transform.forward, damagerPos - transform.position, Vector3.up));
		if (isLocalPlayer) {
			PlayerHUD.Instance.UpdateHealth (health);

			Vector3 dir = (damagerPos - transform.position).normalized;
			//dir = transform.TransformDirection (dir);
			float angle = 90 - (Mathf.Atan2 (dir.z, dir.x) * Mathf.Rad2Deg);
			//print (angle);

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
		PlayerHUD.Instance.ClientReceiveChat (msg);
	}

	[Server]
	private void OnKill () {
		kills++;
		RpcOnKill (kills);
	}

	[ClientRpc]
	private void RpcOnKill (int kills) {
		if (!isLocalPlayer)
			return;
		PlayerHUD.Instance.UpdateKills (netId, kills);
	}

	[Server]
	private void Die (GameObject murderer, BodyPartType bdt) {
		if (isDead)
			return;
		isDead = true;
		murderer.GetComponent<Player> ().OnKill ();

		deaths++;
		GetComponent<WeaponManager> ().DropAllWeapons ();
		gameObject.SetActive (false);
		RpcDie (murderer, bdt, deaths);
	}

	[ClientRpc]
	private void RpcDie (GameObject murderer, BodyPartType bdt, int deaths) {
		PlayerHUD.Instance.UpdateKillFeedList (bdt == BodyPartType.Head ?
			murderer.name + " Head Shotted " + gameObject.name :
			murderer.name + " Killed " + gameObject.name);

		if (isLocalPlayer) {
			PlayerHUD.Instance.UpdateDeaths (netId, deaths);
			PlayerHUD.Instance.DisplayDeathUI (murderer);
		}

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
		gameObject.SetActive (true);
		RpcRespawn (health);
	}

	[ClientRpc]
	private void RpcRespawn (int health) {
		// TODO respawn animation
		Destroy (ragdollInstance, 1);
		if (isLocalPlayer)
			PlayerHUD.Instance.UpdateHealth (health);
		transform.position = NetworkManager.singleton.GetStartPosition ().position;
		gameObject.SetActive (true);
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
	public void CmdDespawn (GameObject gm) {
		NetworkServer.Destroy (gm);
	}

	[Command]
	public void CmdFire () {
		// HACK very expensive way, find better way.
		// right now all raycast calculations are being done one the server
		// so due to the host is also server architecture, the host wont be
		// shot by the clients, this fixes it in a rather expensive manner
		foreach (var child in model.GetComponentsInChildren<Transform> ()) {
			child.gameObject.layer = LayerMask.NameToLayer ("Shooting Player");
		}
		//(weaponManager.HoldingWeapon as GunLegacy).ServerTryFire (shootableLayer);
		foreach (var child in model.GetComponentsInChildren<Transform> ()) {
			child.gameObject.layer = LayerMask.NameToLayer ("Default");
		}
	}

	[ClientRpc]
	private void RpcInitialize (string name, int health, int kills, int deaths) {
		// UI feeback when a player joins
		this.name = name;
		PlayerHUD.Instance.AddPlayerToScoreboard (GetComponent<NetworkIdentity> (), name, kills, deaths);
		if (!isLocalPlayer)
			return;
		PlayerHUD.Instance.UpdateHealth (health);
	}

	[ClientRpc]
	private void RpcUpdateHealth (int health) {
		if (!isLocalPlayer)
			return;
		PlayerHUD.Instance.UpdateHealth (health);
	}

	[ClientRpc]
	private void RpcUpdateKills (int kills) {
		if (!isLocalPlayer)
			return;
		PlayerHUD.Instance.UpdateKills (netId, kills);
	}

	[ClientRpc]
	private void RpcUpdateDeaths (int deaths) {
		if (!isLocalPlayer)
			return;
		PlayerHUD.Instance.UpdateDeaths (netId, deaths);
	}

}