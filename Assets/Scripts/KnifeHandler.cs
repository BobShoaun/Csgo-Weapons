
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

public class KnifeHandler : Handler {

	// clients
	private GameObject firstPersonKnife;
	private Animator animator;

	private Vector3 position;
	private Quaternion rotation;
	private Transform knifeTransform;

	// server
	public Transform look;
	private Knife knife;
	private float nextAttackTime;


	public void Keep () {
		if (animator) {
			animator.Rebind ();
			//print ("rebinding");
		}
	}

	protected override void ClientKeep () {
		knifeTransform.localPosition = position;
		knifeTransform.localRotation = rotation;
	}

	protected override void ServerDeploy () {
		knife = GetComponent<WeaponManager2> ().CurrentWeapon as Knife;
		nextAttackTime = Time.time + knife.deployDuration;
		RpcCrosshair (knife.showCrosshair);
		RpcUpdateUI (0, 0, knife.Name);
	}

	protected override void ClientDeploy () {
		knifeTransform = GetComponent<WeaponManager2> ().HoldingWeapon.transform;

		animator = knifeTransform.GetComponent<Animator> ();
		//animator.Rebind ();
		position = knifeTransform.localPosition;
		rotation = knifeTransform.localRotation;
	}

	protected override void ClientUpdate () {
		if (!isLocalPlayer)
			return;

		if (Input.GetMouseButtonDown (0))
			CmdSwing ();
		else if (Input.GetMouseButtonDown (1))
			CmdStab ();

		if (Input.GetKeyDown (KeyCode.H))
			Keep ();
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
				part.player.CmdTakeDamage (knife.swingDamage, part.bodyPartType, 
					gameObject, transform.position);
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
			var part = hit.collider.GetComponent<BodyPart> ();
			if (part)
				part.player.CmdTakeDamage (knife.stabDamage, part.bodyPartType, 
					gameObject, transform.position);
		}
		RpcStab ();
	}

	[ClientRpc]
	private void RpcSwing () {
		animator.SetTrigger ("Swing");
	}

	[ClientRpc]
	private void RpcStab () {
		animator.SetTrigger ("Stab");
	}

}