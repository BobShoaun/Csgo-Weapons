using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

public abstract class HeldWeapon : MonoBehaviour {
	
	[SerializeField]
	private Weapon weapon;
	public Weapon Weapon {
		get { return weapon; }
	}

	[SerializeField]
	private GameObject droppedPrefab;
	public GameObject DroppedPrefab {
		get { return droppedPrefab; }
	}

	[SerializeField]
	protected float deployTime = 1;

	public virtual void Deploy () {
		// called when the weapon is deployed
	}

	[SerializeField]
	protected bool showCrossHair = true;

	protected virtual void OnEnable () {
		PlayerHUD.Instance.crossHair.SetActive (showCrossHair);
	}

	protected virtual void OnDisable () {
		PlayerHUD.Instance.crossHair.SetActive (true);
	}

}