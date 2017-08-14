using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponSway : MonoBehaviour {

	public float kickBackAmount = 5;
	CharacterController cc;
	public float bobSpeed = 20;
	public float bobAmount = 0.1f;
	private float timer;

	void Start () {
		cc = GetComponentInParent<CharacterController> ();
	}

	void Update () {
		Vector3 velo = cc.velocity;
		velo.y = 0;
		timer += Time.deltaTime;
		float speedPercent = velo.sqrMagnitude / 49f;

		float xOffset = Mathf.Sin (timer * bobSpeed) * bobAmount * speedPercent;
		float zOffset = Mathf.Cos (timer * bobSpeed * 2) * bobAmount * speedPercent;


		Vector3 newPos = new Vector3 (xOffset, 0, (speedPercent * -kickBackAmount) + zOffset);
		transform.localPosition = newPos;



	}
}
