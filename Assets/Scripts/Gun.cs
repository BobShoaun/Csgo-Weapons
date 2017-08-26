using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu]
public class Gun : Weapon {

	public enum Scope { None, Generic, Unique } 

	// static properties
	public float fireRate = 10;
	public int damage = 30;
	public int magazineCapacity = 30;
	public int reservedCapacity = 90;

	public float baseInnacuracy = 0;
	public float accuracyDecay = 0.001f;

	public float recoilScale = 50;
	public float recoilCooldown = 0.25f;
	public Recoil recoil;

	public float reloadDuration = 2.5f;
	public bool continuousReload = false;

	public int bulletsPerShot = 1;
	public int bulletTracerFrequency = 3;
	public bool continuousFire = true;

	public Scope scope = Scope.None;
	public bool unscopeAfterFiring = false;

	public GameObject bulletHolePrefab;
	public GameObject bulletTracerPrefab;

	// runtime properties
	[NonSerialized]
	public int ammunitionInMagazine = 0;
	[NonSerialized]
	public int reservedAmmunition = 0;

	private void OnEnable () {
		ammunitionInMagazine = magazineCapacity;
		reservedAmmunition = reservedCapacity;
		recoil.Initialize (magazineCapacity);
	}

}