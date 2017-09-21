using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityRandom = UnityEngine.Random;
using Doxel.Utility.ExtensionMethods;

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

	public float recoilCooldown = 0.25f;
	public Recoil recoil;

	public float range = 100;
	public int penetrationPower = 1;
	public int armorPenetration = 2;

	public float clipReadyReloadDuration = 1.5f;
	public float fireReadyReloadDuration = 2.5f;
	public bool continuousReload = false;

	public int bulletsPerShot = 1;
	public int tracerBulletInterval = 3;
	public bool continuousFire = true;

	public Scope scope = Scope.None;
	public bool smartScope = false;

	public GameObject bulletHolePrefab;
	public GameObject bulletTracerPrefab;

	public AudioClip shoot;
	public AudioClip reload;

	// runtime persistent properties
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


[Serializable]
public class Recoil {
	// TODO: make this class responsible for the innacuracy,
	// and decay of it ?

	// TODO condiser recoil without storing data but generating pseudo random directions with a seed

	[SerializeField]
	private AnimationCurve patternX;
	[SerializeField]
	private AnimationCurve patternY;
	[SerializeField]
	private int seed = 0;
	[SerializeField]
	private float scale = 50;
	[SerializeField]
	private bool random = false;

	private System.Random randomGenerator;
	private Vector2 [] pattern;
	private int index;
	private int [] randomIndices;

	public void MoveNext () {
//		if (++index >= pattern.Length)
//			Reset ();
	}

	public void Reset () {
		randomGenerator = new System.Random (seed);
		index = -1;
		if (random) {
			randomIndices = new int [pattern.Length];
			randomIndices [0] = 0;
			for (int i = 1; i < pattern.Length; i++) {
				int ranNum = UnityEngine.Random.Range (1, pattern.Length);
				randomIndices [i] = ranNum;
			}
		}
	}
//		
//	private Vector2 Next {
//		get { 
//			int nextIndex = index + (index < pattern.Length - 1 ? 1 : 0);
//			return pattern [random ? randomIndices [nextIndex] : nextIndex];
//		}
//	}

	Vector2 direction;

	private Vector2 Current {
		get {
			// TODO make first recoil at zero zero
			direction = new Vector2 (randomGenerator.NextSingle (-2, 2), 
				2f).normalized;
			return direction;
		}
		//get { return pattern [random ? randomIndices [index] : index]; }
	}

	public Vector3 Rotation {
		get { 
			Vector2 current = Current;
			Debug.Log (current);
			return new Vector3 (-current.y, current.x) 
				//* ((float)randomGenerator.NextDouble () * 2) 
				* scale; 
		}
	}

//	public Vector2 Direction {
//		get { 
//			if (random)
//				return Next.normalized;
//			return (Next - Current).normalized;
//		}
//	}

	public void Initialize (int magAmmo) {
		randomGenerator = new System.Random (seed);
		//UnityRandom.InitState (seed);
		pattern = new Vector2 [magAmmo];
		float timeValue;
		int i;
		for (i = 0, timeValue = 0; i < magAmmo; i++, timeValue += 1f / magAmmo) {
			pattern [i] = new Vector2 (patternX.Evaluate (timeValue), patternY.Evaluate (timeValue));
		}
		Reset ();
	}

}