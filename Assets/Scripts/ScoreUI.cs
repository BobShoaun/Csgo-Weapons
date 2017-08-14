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

	public void UpdateUI (string name, int kills, int deaths) {
		this.name.text = name;
		this.kills.text = "Kills : " + kills;
		this.deaths.text = "Deaths : " + deaths;
	}

}
