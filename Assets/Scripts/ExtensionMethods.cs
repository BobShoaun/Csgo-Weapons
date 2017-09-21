using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Doxel.Utility.ExtensionMethods {

	public static class ActionExtensionMethods {

		public static void Raise (this Action action) {
			if (action != null)
				action ();
		}

		public static void Raise<T> (this Action<T> action, T argument) {
			if (action != null)
				action (argument);
		}

		public static void Raise<T1, T2> (this Action<T1, T2> action, T1 argument1, T2 argument2) {
			if (action != null)
				action (argument1, argument2);
		}

		public static void Raise<T1, T2, T3> (this Action<T1, T2, T3> action, T1 argument1, T2 argument2, T3 argument3) {
			if (action != null)
				action (argument1, argument2, argument3);
		}

		public static IEnumerator DelayedInvoke (this Action action, float duration) {
			yield return new WaitForSeconds (duration);
			action.Raise ();
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

	public static class RandomExtensionMethods {

		public static double NextDouble (this System.Random random, double minimum, double maximum) {
			return random.NextDouble () * (maximum - minimum) + minimum;
		}

		public static float NextSingle (this System.Random random, float minimum, float maximum) {
			return (float) random.NextDouble (minimum, maximum);
		}

	}
		
}