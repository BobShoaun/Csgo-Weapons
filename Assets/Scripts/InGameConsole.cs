using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InGameConsole : SingletonMonoBehaviour<InGameConsole> {

	Text logger;

	void Start () {
		logger = GetComponent<Text> ();
	}

	public void Log (object message) {
		StopCoroutine (Clear ());
		logger.text = message.ToString ();
		StartCoroutine (Clear ());
	}

	private IEnumerator Clear () {
		yield return new WaitForSeconds (2.5f);
		logger.text = "";
	}

}
