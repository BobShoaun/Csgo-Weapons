using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class Knife : Weapon {
	
	public float swingCooldown = 0.5f;
	public float stabCooldown = 1;
	public int swingDamage = 25;
	public int stabDamage = 50;

}