using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreUI : MonoBehaviour {

	[SerializeField]
	private Text name;
	[SerializeField]
	private Text kills;	
	[SerializeField]
	private Text deaths;

	public void SetName (string name, bool isLocalPlayer) {
		this.name.text = name + (isLocalPlayer ? " (You)" : string.Empty);
	}

	public int Kills {
		set {
			kills.text = "Kills : " + value;
		}
	}

	public int Deaths {
		set { 
			deaths.text = "Deaths : " + value;
		}
	}
}
