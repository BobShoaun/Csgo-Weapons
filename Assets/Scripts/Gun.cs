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
	public int tracerBulletInterval = 3;
	public bool continuousFire = true;

	public Scope scope = Scope.None;
	public bool smartScope = false;

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


[Serializable]
public class Recoil : IEnumerator<Vector2> {
	// TODO: make this class responsible for the innacuracy,
	// and decay of it ?

	[SerializeField]
	private AnimationCurve patternX;
	[SerializeField]
	private AnimationCurve patternY;
	[SerializeField]
	private bool random = false;

	private Vector2 [] pattern;
	private int index;
	private int [] randomIndices;

	public bool MoveNext () {
		return ++index < pattern.Length;
	}

	public void Reset () {
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

	object IEnumerator.Current {
		get { return Current; }
	}

	public void Dispose () {

	}

	public Vector2 Next {
		get { 
			int nextIndex = index + (index < pattern.Length - 1 ? 1 : 0);
			return pattern [random ? randomIndices [nextIndex] : nextIndex];
		}
	}

	public Vector2 Current {
		get {
			return pattern [random ? randomIndices [index] : index];
		}
	}

	public Vector2 Direction {
		get { 
			if (random)
				return Next.normalized;
			return (Next - Current).normalized;
		}
	}

	public void Initialize (int magAmmo) {
		pattern = new Vector2 [magAmmo];
		float timeValue;
		int i;
		for (i = 0, timeValue = 0; i < magAmmo; i++, timeValue += 1f / magAmmo) {
			pattern [i] = new Vector2 (patternX.Evaluate (timeValue), patternY.Evaluate (timeValue));
		}
		Reset ();
	}

}