using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponSway : MonoBehaviour {

	public float kickBackAmount = 5;

	public float bobSpeed = 20;
	public float bobAmount = 0.1f;
	public float swayAmount = 0.02f;
	public float swaySpeed = 7;

	private CharacterController cc;
	private float timer;
	private Vector3 sway;

	void Start () {
		cc = GetComponentInParent<CharacterController> ();
	}

	void Update () {
		Vector3 velocity = cc.velocity;
		velocity.y = 0;
		timer += Time.deltaTime;
		float speedPercent = velocity.sqrMagnitude / 49f;

		Vector3 bob = new Vector3 (Mathf.Sin (timer * bobSpeed), 
			              Mathf.Cos (timer * bobSpeed * 2), 
			              Mathf.Cos (timer * bobSpeed * 2))
		              * bobAmount * speedPercent;

		Vector3 kickback = new Vector3 (0, 1, 1) * speedPercent * -kickBackAmount;

		Vector2 swayDelta = new Vector2 (Input.GetAxis ("Mouse X"), Input.GetAxis ("Mouse Y")) * -swayAmount;

		sway = Vector2.Lerp (sway, swayDelta, Time.deltaTime * swaySpeed);
		//swayOffset = Vector2.MoveTowards (swayOffset, sway, Time.deltaTime * 3);
		transform.localPosition = sway + bob + kickback;
	}

}
