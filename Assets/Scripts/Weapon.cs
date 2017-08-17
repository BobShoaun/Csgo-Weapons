using UnityEngine;

[CreateAssetMenu (menuName = "Weapon")]
public class Weapon : ScriptableObject {

	public enum SlotType {
		Primary,
		Secondary,
		Knife,
		Flash,
		Grenade,
		Smoke,
		Decoy,
		Bomb
	}

	/// <summary>
	/// IMPORTANT: Name of the weapon, not the gameObject !!
	/// </summary>
	[SerializeField]
	private new string name;
	public string Name { 
		get { return name; }
	}

	[SerializeField]
	private SlotType slot;
	public SlotType Slot { 
		get { return slot; }
	}
		
}

public class WeaponH {

	public GameObject firstPersonPrefab;
	public GameObject thirdPersonPrefab;
	public GameObject droppedPrefab;

	public void Equip () {
		
	}
		
	public void Drop () {
		
	}

	public void Deploy () {
		
	}

	public void Keep () {
		
	}

}

