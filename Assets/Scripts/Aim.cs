using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using DUtil = Doxel.Utility.Utility;

public class Aim : NetworkBehaviour {
	 
	// Client
	private View view;

	// Server
	[SerializeField]
	private Transform aim;
	private Vector3 localEulerAngles;

	public Vector3 Origin {
		get { return aim.position; }
	}

	public Vector3 Direction {
		get { return aim.forward; }
	}

	[ClientCallback]
	private void Start () {
		view = GetComponentInChildren<View> ();
	}

	[ServerCallback]
	private void Update () {
		localEulerAngles = DUtil.ExponentialDecayTowards (localEulerAngles, Vector3.zero, Mathf.Exp (-1), Time.deltaTime);
		aim.localRotation = Quaternion.Euler (localEulerAngles);
	}

	[Server]
	public void AddRotation (Vector3 rotationToAdd) {
		localEulerAngles += rotationToAdd;
		RpcSetViewRotation (localEulerAngles);
	}

	[ClientRpc]
	private void RpcSetViewRotation (Vector3 rotation) {
		view.recoilTrackingRotation = rotation;
	}

}