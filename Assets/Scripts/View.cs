using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class View : MonoBehaviour {

	public float recoilTrackingScale = .45f;
	public float punchAmount = .055f;
	public float punchDecay = 18;

	public Vector2 recoilTrackingRotation;
	public Vector2 punchRotation;

	private void Update () {
		transform.localRotation = Quaternion.Euler (recoilTrackingRotation * recoilTrackingScale);
	}

}
