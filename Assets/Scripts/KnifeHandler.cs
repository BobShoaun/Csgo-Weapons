
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

	[ClientCallback]
	private void Awake () {

	}

	private void OnEnable () {
		if (isClient) {

			knifeTransform = GetComponent<WeaponManager2> ().HoldingWeapon.transform;
	
			animator = knifeTransform.GetComponent<Animator> ();
			position = knifeTransform.localPosition;
			rotation = knifeTransform.localRotation;
		}
		if (isServer) {
			knife = GetComponent<WeaponManager2> ().CurrentWeapon as Knife;
			RpcUpdateUI (0, 0, knife.Name);
		}
	}

	[ClientCallback]
	private void OnDisable () {
		knifeTransform.localPosition = position;
		knifeTransform.localRotation = rotation;
	}

	[ClientCallback]
	private void Update () {
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