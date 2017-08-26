using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RuntimeWeaponDatabase : RuntimeDatabase<Weapon, RuntimeWeaponDatabase> {

	public WeaponDatabase weaponDatabase;

	protected override void Awake () {
		base.Awake ();

	}

	public Weapon Add (int weaponId) {
		Weapon newWeapon = (Weapon) weaponDatabase [weaponId].Clone ();
		newWeapon.runtimeId = Add (newWeapon);
		return newWeapon;
	}

}