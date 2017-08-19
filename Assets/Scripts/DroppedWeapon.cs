using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

public class DroppedWeapon : NetworkBehaviour {

	public int weaponId;

	public DynamicWeapon DynamicWeapon;

	protected virtual void Start () {
		if (DynamicWeapon == null) {
			Weapon2 weapon = WeaponDatabase.Instance [weaponId];
			if (weapon is Gun2)
				DynamicWeapon = new DynamicGun (weapon as Gun2);
			else if (weapon is Knife2)
				DynamicWeapon = new DynamicWeapon (weapon);
		}
	}

}