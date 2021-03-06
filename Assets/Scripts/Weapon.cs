using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public abstract class Weapon : ScriptableObject, IIdentifiable {

	public enum SlotType { Primary, Secondary, Knife,
		Flash, Grenade, Smoke, Decoy, Bomb }

	[SerializeField]
	private int id;
	public int Id {
		get { return id; }
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

	[SerializeField]
	private GameObject firstPersonPrefab;
	public GameObject FirstPersonPrefab {
		get { return firstPersonPrefab; }
	}

	[SerializeField]
	private GameObject thirdPersonPrefab;
	public GameObject ThirdPersonPrefab {
		get { return thirdPersonPrefab; }
	}

	[SerializeField]
	private DroppedWeapon droppedPrefab;
	public DroppedWeapon DroppedPrefab {
		get { return droppedPrefab; }
	}
		
	public int mobility = 50;
	public int price = 1000;
	public int killReward = 300;
	public float deployDuration = 1.5f;
	public bool showCrosshair = true;

}