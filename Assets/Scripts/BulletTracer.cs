using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletTracer : MonoBehaviour {

	void Start () {
		GetComponent<Rigidbody> ().velocity = transform.forward * 200;
		Destroy (gameObject, 5);
	}

	void Update () {
		//transform.Translate (Vector3.forward * 10 * Time.deltaTime);
	}

	void OnTriggerEnter () {
		Destroy (gameObject);
	}

}