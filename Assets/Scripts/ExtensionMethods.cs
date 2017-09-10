using System;
using System.Collections;
using System.Collections.Generic;
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
			return Array.Find (gameObject.GetComponentsInParent<Transform> (includeInactive), transform => transform.name == name).gameObject;
		}

		public static GameObject GetGameObjectInChildren (this GameObject gameObject, string name, bool includeInactive = false) {
			return Array.Find (gameObject.GetComponentsInChildren<Transform> (includeInactive), transform => transform.name == name).gameObject;
		}

	}
		
}