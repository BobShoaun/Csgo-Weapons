using UnityEngine;

public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : SingletonMonoBehaviour<T> {

	public static T Instance { get; private set; }

	protected virtual void Awake () {
		if (Instance) {
			Destroy (gameObject);
			return;
		}
		DontDestroyOnLoad (transform.root);
		Instance = this as T;
	}

}