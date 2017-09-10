using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class HighExplosiveGrenade : DroppedWeapon, IGrenade {

	// Both
	public Collider sphere;
	public Collider capsule;

	// Server
	public float timer = 1.7f;
	public float radius = 5;
	public float explosionForce = 100;
	private Player playerPrimer;
	private bool primed = false;

	//private int bounceCount = 0;

	[ServerCallback]
	protected override void Start () {
		if (!primed)
			base.Start ();
		//sphere = GetComponent<SphereCollider> ();
		//capsule = GetComponent<CapsuleCollider> ();
	}

	[Server]
	public void Prime (Player primer) {
		primed = true;
		playerPrimer = primer;
		// TODO: have the physics calculations be both local and in server
		// so it will look more responsive
		capsule.enabled = false;
		sphere.enabled = true;
		RpcChangeShape (true, false);
		StartCoroutine (Timer ());
	}

	[Server]
	private IEnumerator Timer () {
		yield return new WaitForSeconds (timer / 2);
		sphere.enabled = false;
		capsule.enabled = true;
		RpcChangeShape (false, true);
		yield return new WaitForSeconds (timer / 2);
		Explode ();
	}
//
//	[Server]
//	private void OnCollisionEnter () {
//		if (!primed)
//			return;
//		//bounceCount++;
//		if (bounceCount > 2)
//		Explode ();
//	}

	[Server]
	public void Explode () {
		foreach (var col in Physics.OverlapSphere (transform.position, 5)) {
			if (col.attachedRigidbody)
				col.attachedRigidbody.AddExplosionForce (1000, transform.position, 5, 5);
			BodyPart bodyPart;
			if (bodyPart = col.GetComponent<BodyPart> ())
				bodyPart.TakeDamage (50, playerPrimer.gameObject, transform.position);
		}
		Destroy (gameObject);
	}

	[ClientRpc]
	public void RpcChangeShape (bool sphere, bool capsule) {
		this.sphere.enabled = sphere;
		this.capsule.enabled = capsule;
	}
	
}

public interface IGrenade {

	void Prime (Player primer);

}