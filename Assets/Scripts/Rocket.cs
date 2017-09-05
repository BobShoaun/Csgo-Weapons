using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Rocket : NetworkBehaviour {

	public float force = 10;
	private new Rigidbody rigidbody;
	private float lifeTime = 10;
	private float deathTime;

	[ServerCallback]
	private void Start () {
		rigidbody = GetComponent<Rigidbody> ();
		deathTime = Time.time + lifeTime;
	}

	[ServerCallback]
	private void Update () {
		if (Time.time >= deathTime)
			Explode ();
	}

	[ServerCallback]
	private void FixedUpdate () {
		//transform.Translate (Vector3.forward * speed * Time.fixedDeltaTime);
		rigidbody.AddForce (transform.forward * force * Time.fixedDeltaTime, ForceMode.Acceleration);
		rigidbody.AddTorque (transform.forward * 100);
	}

	[ServerCallback]
	private void OnCollisionEnter () {
		//Explode ();
	}

	[Server]
	private void Explode () {
		foreach (var col in Physics.OverlapSphere (transform.position, 5)) {
			if (col.attachedRigidbody)
				col.attachedRigidbody.AddExplosionForce (1000, transform.position, 5, 5);
			Player player;
			if (player = col.GetComponent<Player> ()) {
				//player.CmdTakeDamage (50, BodyPartType.Legs, playerPrimer.gameObject, transform.position);
			}
		}
		Destroy (gameObject);
	}
		
}