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

}

public enum BodyPartType { Head, UpperTorso, LowerTorso, Legs }