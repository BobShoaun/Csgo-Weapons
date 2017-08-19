using UnityEngine;

public abstract class SemiSingletonMonoBehaviour<T> : MonoBehaviour where T : SemiSingletonMonoBehaviour<T> {

	public static T Instance { get; private set; }

	protected virtual void Awake () {
		if (Instance)
			Debug.LogError ("Multiple SemiSingletonMonoBehaviours of the same type found in the scene.");
		else
			Instance = this as T;
	}

}