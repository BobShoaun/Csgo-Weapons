using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class LauncherHandler : Handler {

	private Launcher launcher;
	private float nextFireTime = 0;

	[ServerCallback]
	private void OnEnable () {
		launcher = GetComponent<WeaponManager2> ().CurrentWeapon as Launcher;
	}

	[ClientCallback]
	private void Update () {
		if (Input.GetMouseButtonDown (0))
			CmdFire ();
	}

	[Command]
	private void CmdFire () {
		if (Time.time < nextFireTime)
			return;
		nextFireTime = Time.time + 1 / launcher.fireRate;
		Instantiate (launcher.projectilePrefab);

	}

}