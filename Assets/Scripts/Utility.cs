// ***Copyright © 2017 Doxel aka Ng Bob Shoaun. All Rights Reserved.***

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityObject = UnityEngine.Object;
using Doxel.Utility.ExtensionMethods;

namespace Doxel.Utility {
	
	public static class Utility {

		public static Color translucent = new Color (1, 1, 1, 0.5f);

		public static int Remainder (int dividend, int divisor) {
			return (dividend % divisor + divisor) % divisor;
		}

		public static void Swap<T> (ref T value1, ref T value2) {
			T tempValue = value1;
			value1 = value2;
			value2 = tempValue;
		}

		public static T[] FindObjectsOfNonUnityType<T> () where T : class {
			var objectsOfNonUnityType = new List<T> ();
			foreach (var gameObject in UnityObject.FindObjectsOfType<GameObject> ()) {
				var objectOfNonUnityType = gameObject.GetComponents<T> ();
				if (objectOfNonUnityType != null)
					objectsOfNonUnityType.AddRange (objectOfNonUnityType);
			}
			return objectsOfNonUnityType.ToArray ();
		}

		public static IEnumerator DelayedInvoke (Action method, float delay) {
			yield return new WaitForSeconds (delay);
			method ();
		}

		public static IEnumerator DelayedInvokeRealTime (Action method, float delay) {
			yield return new WaitForSecondsRealtime (delay);
			method ();
		}

		public static IEnumerator Fade (Action<Color> result, float duration, Color initial, Color target, Action callback = null) {
			for (float percent = 0; percent <= 1; percent += Time.unscaledDeltaTime / duration) {
				result (Color.Lerp (initial, target, percent));
				yield return null;
			}
			result (target);
			callback.Raise ();
		}

		public static IEnumerator Fade (Action<Color> result, float duration, Color initial, Color target, AnimationCurve curve, Action callback = null) {
			for (float percent = 0; percent <= 1; percent += Time.unscaledDeltaTime / duration) {
				result (Color.Lerp (initial, target, curve.Evaluate (percent)));
				yield return null;
			}
			result (target);
			callback.Raise ();
		}

		public static IEnumerator Fade (Action<float> result, float duration, float initial, float target, Action callback = null) {
			for (float percent = 0; percent <= 1; percent += Time.unscaledDeltaTime / duration) {
				result (Mathf.Lerp (initial, target, percent));
				yield return null;
			}
			result (target);
			callback.Raise ();
		}

		public static IEnumerator Fade (Action<float> result, float duration, float initial, float target, AnimationCurve curve, Action callback = null) {
			for (float percent = 0; percent <= 1; percent += Time.unscaledDeltaTime / duration) {
				result (Mathf.Lerp (initial, target, curve.Evaluate (percent)));
				yield return null;
			}
			result (target);
			callback.Raise ();
		}

		public static IEnumerator Transition (Action<Vector2> result, float duration, Vector2 initial, Vector2 target, Action callback = null) {
			for (float percent = 0; percent <= 1; percent += Time.unscaledDeltaTime / duration) {
				result (Vector2.Lerp (initial, target, percent));
				yield return null;
			}
			result (target);
			callback.Raise ();
		}
			
	}

}