using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

public class DroppedWeapon : NetworkBehaviour {

	// Client

	// Server
	public Weapon weapon;

	protected virtual void Start () {
		name = weapon.name;
	}

}