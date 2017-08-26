using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

public class DroppedWeapon : NetworkBehaviour {

	// Client
	[NonSerialized]
	public string name; // TODO no need variable, just set the gameobject's name

	// Server
	public Weapon weapon;

	[ServerCallback]
	protected virtual void Start () {
		if (weapon != null)
			RpcName (weapon.Name);
	}

	private void RpcName (string name) {
		this.name = name;
	}

}