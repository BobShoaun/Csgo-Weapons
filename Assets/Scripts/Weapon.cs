using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[CreateAssetMenu (menuName = "Weapon")]
public class Weapon : ScriptableObject, IIdentifiable {

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
	private GameObject droppedPrefab;
	public GameObject DroppedPrefab {
		get { return droppedPrefab; }
	}

}


public class DynamicWeapon {

	public Weapon2 weapon;

	public DynamicWeapon () {}

	public DynamicWeapon (Weapon2 w) {
		weapon = w;	
	}

}

public class DynamicGun : DynamicWeapon {

	public int ammunitionInMagazine = 0;
	public int reservedAmmunition = 0;

	public DynamicGun (Gun2 w) : base (w) {
		ammunitionInMagazine = w.magazineCapacity;
		reservedAmmunition = w.reservedCapacity;
	}

}

public class Weapon2 : IIdentifiable {

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
	private GameObject droppedPrefab;
	public GameObject DroppedPrefab {
		get { return droppedPrefab; }
	}

	public Weapon2 (int id, string name, SlotType slot, GameObject firstPersonPrefab, GameObject thirdPersonPrefab, GameObject droppedPrefab) {
		this.id = id;
		this.name = name;
		this.slot = slot;
		this.firstPersonPrefab = firstPersonPrefab;
		this.thirdPersonPrefab = thirdPersonPrefab;
		this.droppedPrefab = droppedPrefab;
	}

}

public class Knife2 : Weapon2 {

	public float swingCooldown = 0.5f;
	public float stabCooldown = 1;
	public int swingDamage = 25;
	public int stabDamage = 50;

	public Knife2 (int id, string name, SlotType slot, GameObject firstPersonPrefab, GameObject thirdPersonPrefab, GameObject droppedPrefab,
		float swingCooldown, float stabCooldown, int swingDamage, int stabDamage) :
	base (id, name, slot, firstPersonPrefab, thirdPersonPrefab, droppedPrefab) {
		this.swingCooldown = swingCooldown;
		this.stabCooldown = stabCooldown;
		this.swingDamage = swingDamage;
		this.stabDamage = stabDamage;
	}

}

public class Gun2 : Weapon2 {
	
	public enum Scope { None, Generic, Unique } 

	public float fireRate = 10;
	public int magazineCapacity = 30;
	public int reservedCapacity = 90;
	public GameObject bulletHoldPrefab;
	public GameObject bulletTracerPrefab;
	public bool continuousFire = true;
	public int damage = 30;
	public float baseInnacuracy = 0;
	public float accuracyDecay = 0.001f;
	public int bulletsPerShot = 1;
	public float recoilScale = 50;
	public float recoilCooldown = 0.25f;
	public float reloadDuration = 2.5f;
	public bool continuousReload = false;
	public Scope scope = Scope.None;
	public int bulletTracerFrequency = 3;
	public Recoil recoil;

	public Gun2 (int id, string name, SlotType slot, GameObject firstPersonPrefab, GameObject thirdPersonPrefab, GameObject droppedPrefab,
		float fireRate, int magazineCapacity, int reservedCapacity, bool continuousFire, int damage, Recoil recoil) : 
		base (id, name, slot, firstPersonPrefab, thirdPersonPrefab, droppedPrefab) {
		this.fireRate = fireRate;
		this.magazineCapacity = magazineCapacity;
		this.reservedCapacity = reservedCapacity;
		this.continuousFire = continuousFire;
		this.damage = damage;
		this.recoil = recoil;
		recoil.Initialize (magazineCapacity);
	}

}