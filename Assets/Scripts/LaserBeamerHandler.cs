using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Doxel.Utility.ExtensionMethods;

public class LaserBeamerHandler : Handler {

	// Client
	private Transform firstPersonMuzzle;
	private Transform thirdPersonMuzzle;
	private LineRenderer laser;

	// Server
	[SerializeField]
	private LayerMask shootableLayer;
	public Transform recoilTransform;
	private LaserBeamer laserBeamer;
	private bool shooting;
	private int damage;
	private float nextDamageIncreaseTime = 0;
	private float nextShootTime = 0;

	protected override Type WeaponType {
		get {
			return typeof (LaserBeamer);
		}
	}

	[ServerCallback]
	private void Start () {
		
	}

	protected override void ServerDeploy (Weapon weapon) {
		base.ServerDeploy (weapon);
		laserBeamer = weapon as LaserBeamer;

		damage = laserBeamer.baseDamage;
		nextShootTime = Time.time + laserBeamer.deployDuration;
	}

	protected override void ServerKeep () {
		shooting = false;
	}

	protected override void ClientDeploy (Weapon weapon) {
		base.ClientDeploy (weapon);
		if (isLocalPlayer) {
			firstPersonMuzzle = firstPersonViewmodel.GetGameObjectInChildren ("Muzzle").transform;
			laser = firstPersonMuzzle.GetComponent<LineRenderer> ();
		}
		else
			thirdPersonMuzzle = thirdPersonWeaponModel.GetGameObjectInChildren ("Muzzle").transform;
	}

	protected override void ServerUpdate () {
		if (shooting && Time.time >= nextShootTime)
			RpcShoot (RaycastReflect (recoilTransform.position, 
				recoilTransform.forward, laserBeamer.range).ToArray ());
	}

	protected override void ClientUpdate () {
		if (Input.GetMouseButtonDown (0))
			CmdToggleShoot (true);
		else if (Input.GetMouseButtonUp (0))
			CmdToggleShoot (false); 
	}

	[Command]
	private void CmdToggleShoot (bool shooting) {
		this.shooting = shooting;
		RpcToggleShoot (shooting);
	}

	[ClientRpc]
	private void RpcToggleShoot (bool shooting) {
		if (isLocalPlayer)
			laser.enabled = shooting;
	}

	[ClientRpc]
	private void RpcShoot (Vector3 [] reflections) {
		reflections = Array.ConvertAll (reflections, 
			reflection => firstPersonMuzzle.InverseTransformPoint (reflection));
		if (isLocalPlayer) {
			laser.positionCount = reflections.Length + 1;
			for (int i = 0; i < reflections.Length; i++)
				laser.SetPosition (i + 1, reflections [i]);
		}
	}

	[Server]
	private IEnumerable<Vector3> RaycastReflect (Vector3 position, Vector3 direction, float range, ICollection<Vector3> reflections = null) {
		if (range <= 0)
			return reflections;
		if (reflections == null)
			reflections = new List<Vector3> ();
		RaycastHit raycastHit;
		if (Physics.Raycast (position, direction, out raycastHit, range, shootableLayer)) {
			BodyPart bodyPart;
			if (bodyPart = raycastHit.collider.GetComponent<BodyPart> ()) {
				bodyPart.TakeDamage (laserBeamer.baseDamage, gameObject, transform.position);
				nextDamageIncreaseTime = Time.time + laserBeamer.damageIncreaseInterval;
				if (nextDamageIncreaseTime >= Time.time) {
					
				}
			}
			Rigidbody rigidbody = raycastHit.rigidbody;
			if (rigidbody) {
				rigidbody.AddForceAtPosition (direction * 50, raycastHit.point);
			}

			reflections.Add (raycastHit.point);
			RaycastReflect (raycastHit.point, Vector3.Reflect (direction, raycastHit.normal), 
				range - raycastHit.distance, reflections);
		}
		else
			reflections.Add (position + direction * range);
		return reflections;
	}

}