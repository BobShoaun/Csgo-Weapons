using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class HighExplosiveGrenade : DroppedWeapon, IGrenade {

	public float timer = 4;
	public float radius = 5;
	public float explosionForce = 100;
	public Collider sphere;
	public Collider capsule;
	private int bounceCount = 0;
	private Player playerPrimer;
	private bool primed = false;

	protected override void Start () {
		base.Start ();
		sphere = GetComponent<SphereCollider> ();
		capsule = GetComponent<CapsuleCollider> ();
	}

	public void Prime (Player primer) {
		primed = true;
		playerPrimer = primer;
		capsule.enabled = false;
		sphere.enabled = true;
		StartCoroutine (Timer ());
	}

	private IEnumerator Timer () {
		yield return new WaitForSeconds (timer / 2);
		sphere.enabled = false;
		capsule.enabled = true;
		yield return new WaitForSeconds (timer / 2);
		Explode ();
	}

	[Server]
	private void OnCollisionEnter () {
		if (!primed)
			return;
		bounceCount++;
		if (bounceCount > 2)
		Explode ();
	}

	public void Explode () {
		foreach (var col in Physics.OverlapSphere (transform.position, 5)) {
			if (col.attachedRigidbody)
				col.attachedRigidbody.AddExplosionForce (1000, transform.position, 5, 5);
			Player player;
			if (player = col.GetComponent<Player> ()) {
				player.CmdTakeDamage (50, BodyPartType.Legs, playerPrimer.gameObject, transform.position);
			}
		}
		Destroy (gameObject);
	}
	
}

public interface IGrenade {

	void Prime (Player primer);

}