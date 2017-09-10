
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

public class KnifeHandler : Handler {

	// clients
	private Animator animator;

	private Vector3 position;
	private Quaternion rotation;
	private Transform knifeTransform;

	// server
	public Transform look;
	private Knife knife;
	private float nextAttackTime;

	protected override Type WeaponType {
		get {
			return typeof (Knife);
		}
	}

	protected override void ClientKeep () {
		base.ClientKeep ();
		//knifeTransform.localPosition = position;
		//knifeTransform.localRotation = rotation;
	}

	protected override void ServerKeep () {
		if (animator) {
			animator.Rebind ();
			//print ("rebinding");
		}
	}

	protected override void ServerDeploy (Weapon weapon) {
		base.ServerDeploy (weapon);
		knife = weapon as Knife;

		nextAttackTime = Time.time + knife.deployDuration;
	}

	protected override void ClientDeploy (Weapon weapon) {
		base.ClientDeploy (weapon);
		if (isLocalPlayer) {
			knifeTransform = firstPersonViewmodel.transform;
			animator = knifeTransform.GetComponent<Animator> ();
		}
		//animator.Rebind ();
		//position = knifeTransform.localPosition;
		//rotation = knifeTransform.localRotation;
	}

	protected override void ClientUpdate () {
		if (!isLocalPlayer)
			return;

		if (Input.GetMouseButtonDown (0))
			CmdSwing ();
		else if (Input.GetMouseButtonDown (1))
			CmdStab ();
	}

	[Command]
	private void CmdSwing () {
		if (Time.time < nextAttackTime)
			return;
		nextAttackTime = Time.time + knife.swingCooldown;
		RaycastHit hit;
		if (Physics.Raycast (look.position, look.forward, out hit, 2)) {
			var part = hit.collider.GetComponent<BodyPart> ();
			if (part)
				part.TakeDamage (knife.swingDamage, gameObject, transform.position);
		}
		RpcSwing ();
	}

	[Command]
	private void CmdStab () {
		if (Time.time < nextAttackTime)
			return;
		nextAttackTime = Time.time + knife.stabCooldown;
		RaycastHit hit;
		if (Physics.Raycast (look.position, look.forward, out hit, 2)) {
			BodyPart bodyPart;
			if (bodyPart = hit.collider.GetComponent<BodyPart> ())
				bodyPart.TakeDamage (knife.stabDamage, gameObject, transform.position);
		}
		RpcStab ();
	}

	[ClientRpc]
	private void RpcSwing () {
		if (isLocalPlayer)
			animator.SetTrigger ("Swing");
	}

	[ClientRpc]
	private void RpcStab () {
		if (isLocalPlayer)
			animator.SetTrigger ("Stab");
	}

}