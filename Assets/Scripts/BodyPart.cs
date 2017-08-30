using System;
using UnityEngine;

[RequireComponent (typeof (Collider))]
public class BodyPart : MonoBehaviour {

	[NonSerialized]
	public Player player;
	public BodyPartType bodyPartType;

	private void Start () {
		player = GetComponentInParent<Player> ();
	}

	public static int CalculateDamage (BodyPartType bodyPart, int damage) {
		switch (bodyPart) {
			case BodyPartType.Head:
				return damage * 4;
			case BodyPartType.UpperTorso:
				return damage;
			case BodyPartType.LowerTorso:
				return damage * 2;
			case BodyPartType.Legs:
				return damage / 2;
			default :
				return damage;
		}
	}

}

public enum BodyPartType { Head, UpperTorso, LowerTorso, Legs }