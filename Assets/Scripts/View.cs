using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class View : MonoBehaviour {

	[NonSerialized]
	public float recoilTrackingScale = 0.45f;
	[NonSerialized]
	public float punchAmount = 1;
	public float punchDecay = 1;

	private new Camera camera;
	public Vector2 recoilTrackingRotation;
	private Vector3 punchRotation;
	public Vector3 PunchDirection {
		set { 
			punchRotation += new Vector3 (-value.y, value.x) * punchAmount;
		}
	}

	public float FieldOfView {
		set { 
			camera.fieldOfView = value;
		}
	}

	private void Start () {
		camera = GetComponent<Camera> ();
	}

	private void Update () {
		//transform.localRotation = Quaternion.Euler (recoilTrackingRotation * recoilTrackingScale);
//		float angleX = recoilTransform.localEulerAngles.x;
//		angleX = angleX > 180 ? angleX - 360 : angleX;
//		float angleY = recoilTransform.localEulerAngles.y;
//		angleY = angleY > 180 ? angleY - 360 : angleY;
		//Debug.Log (new Vector3 (angleX, angleY));
		transform.localRotation = Quaternion.Euler (punchRotation + (Vector3) recoilTrackingRotation * recoilTrackingScale);
		// TODO make this decay exponential
		punchRotation = Vector3.MoveTowards (punchRotation, Vector3.zero, Time.deltaTime * punchDecay);
	}

}
