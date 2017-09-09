using System;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent (typeof (Collider))]
public class BodyPart : MonoBehaviour {

	public NetworkInstanceId NetId {
		get { return player.netId; }
	}
	private Player player;
	[SerializeField]
	private BodyPartType bodyPartType;

	private void Start () {
		player = GetComponentInParent<Player> ();
	}

	private int CalculateDamage (int damage) {
		switch (bodyPartType) {
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

	public void TakeDamage (int damage, GameObject damager, Vector3 damageSourcePosition) {
		player.TakeDamage (CalculateDamage (damage), damager, damageSourcePosition, bodyPartType);
	}

}

public enum BodyPartType { Head, UpperTorso, LowerTorso, Legs }