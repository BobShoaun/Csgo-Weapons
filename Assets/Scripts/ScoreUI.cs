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
	private Text assists;	
	[SerializeField]
	private Text deaths;

	public bool IsLocalPlayer {
		set { name.text += value ? " (You)" : string.Empty; }
	}

	public string Name {
		set { name.text = value; }
	}

	public int Kills {
		set {
			kills.text = "Kills : " + value;
		}
	}

	public int Assists {
		set { assists.text = "Assists : " + value; }
	}

	public int Deaths {
		set { 
			deaths.text = "Deaths : " + value;
		}
	}
}
