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

	public void UpdateUI (string name, int kills, int deaths, bool isYou) {
		this.name.text = name + (isYou ? " (You)" : string.Empty);
		this.kills.text = "Kills : " + kills;
		this.deaths.text = "Deaths : " + deaths;
	}

}
