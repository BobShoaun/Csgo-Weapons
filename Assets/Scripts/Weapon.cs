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

