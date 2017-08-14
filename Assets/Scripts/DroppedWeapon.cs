using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

public class DroppedWeapon : NetworkBehaviour {

	[SerializeField]
	private Weapon weapon;
	public Weapon Weapon {
		get { return weapon; }
	}

	[SerializeField] [FormerlySerializedAs ("heldWeaponPrefab")]
	private GameObject heldPrefab;
	public GameObject HeldPrefab {
		get { return heldPrefab; }
	}

}