using System;
using System.Collections;
using UnityEngine;

namespace Doxel.Utility.ExtensionMethods {

	public static class ActionExtensionMethods {

		public static void Raise (this Action actionToCall) {
			if (actionToCall != null)
				actionToCall ();
		}

		public static void Raise<T> (this Action<T> actionToCall, T argument) {
			if (actionToCall != null)
				actionToCall (argument);
		}

		public static void Raise<T1, T2> (this Action<T1,T2> actionToCall, T1 argument1, T2 argument2) {
			if (actionToCall != null)
				actionToCall (argument1, argument2);
		}

		public static IEnumerator DelayedInvoke (this Action actionToInvoke, float duration) {
			yield return new WaitForSeconds (duration);
			actionToInvoke.Raise ();
		}

	}

	public static class GameObjectExtensionMethods {

		public static GameObject GetGameObjectInParent (this GameObject gameObject, string name, bool includeInactive = false) {
			foreach (var transform in gameObject.GetComponentsInParent<Transform> (includeInactive))
				if (transform.name == name)
					return transform.gameObject;
			Debug.LogError ("No game object called " + name + " is found in parent");
			return null;
		}

		public static GameObject GetGameObjectInChildren (this GameObject gameObject, string name, bool includeInactive = false) {
			foreach (var transform in gameObject.GetComponentsInChildren<Transform> (includeInactive))
				if (transform.name == name)
					return transform.gameObject;
			Debug.LogError ("No game object called " + name + " is found in children");
			return null;
		}

	}
		
}