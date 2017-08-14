using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Doxel.Utility.ExtensionMethods;

public class Knife : HeldWeapon {

	[SerializeField]
	private float swingCooldown = 0.5f;
	[SerializeField]
	private float stabCooldown = 1;

	[SerializeField]
	private int swingDamage = 25;
	[SerializeField]
	private int stabDamage = 50;

	private Player player;
	private Transform look;
	private float nextAttackTime;
	private Animator animator;

	private Vector3 position;
	private Quaternion rotation;

	protected override void OnEnable () {
		base.OnEnable ();
	}

	protected override void OnDisable () {
		base.OnDisable ();
		// just in case the knife is disabled during an animation
		// the position and rot is set to its original
		transform.localPosition = position;
		transform.localRotation = rotation;
	}

	private void Awake () {
		// cache initial pos and rot
		position = transform.localPosition;
		rotation = transform.localRotation;

		player = GetComponentInParent<Player> ();
		look = gameObject.GetGameObjectInParent ("Look").transform;
		animator = GetComponent<Animator> ();
	}

	private void Update () {
		if (!player.isLocalPlayer)
			return;
		
		if (Input.GetMouseButtonDown (0))
			player.CmdSwing ();
		else if (Input.GetMouseButtonDown (1))
			player.CmdStab ();
	
	}

	public override void Deploy () {
		// forces animator to play idle state, basicly resetting
		// the animator
		animator.Play ("Idle");
	}

	public void ServerTrySwing () {
		if (Time.time < nextAttackTime)
			return;
		nextAttackTime = Time.time + swingCooldown;
		RaycastHit hit;
		if (Physics.Raycast (look.position, look.forward, out hit, 2)) {
			//print ("swing : " + hit.collider.gameObject);

			var part = hit.collider.GetComponent<BodyPart> ();
			if (part)
				part.player.CmdTakeDamage (swingDamage, part.bodyPartType, 
					player.gameObject, player.gameObject.transform.position);

//			Player enemy;
//			if (enemy = hit.collider.GetComponent<Player> ()) {
//				enemy.CmdTakeDamage (25, player.gameObject, player.transform.position);
//			}

		}
		player.RpcSwing ();
	}

	public void ServerTryStab () {
		if (Time.time < nextAttackTime)
			return;
		nextAttackTime = Time.time + stabCooldown;
		RaycastHit hit;
		if (Physics.Raycast (look.position, look.forward, out hit, 2)) {
			//print ("stab : " + hit.collider.gameObject);
			var part = hit.collider.GetComponent<BodyPart> ();
			if (part)
				part.player.CmdTakeDamage (stabDamage, part.bodyPartType, 
					player.gameObject, player.gameObject.transform.position);
//			Player enemy;
//			if (enemy = hit.collider.GetComponent<Player> ()) {
//				enemy.CmdTakeDamage (50, player.gameObject, player.transform.position);
//			}

		}
		player.RpcStab ();
	}

	public void ClientSwing () {
		// swing effect animation
		animator.SetTrigger ("Swing");
	}

	public void ClientStab () {
		// stab effect animation
		animator.SetTrigger ("Stab");
	}

}