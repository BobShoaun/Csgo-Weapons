using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Doxel.Environment {

	public class Material : MonoBehaviour {

		public BulletCollisionBehaviour bulletCollisionBehaviour = BulletCollisionBehaviour.Penetrate;
		public bool showBulletHole = true;
		public int penetrationPrevention = 5;
		public GameObject bulletHolePrefab;
		public AudioClip footStepSound;

		private void OnValidate () {
			if (bulletCollisionBehaviour != BulletCollisionBehaviour.Penetrate) {
				penetrationPrevention = 0;
			}
		}

	}

	public enum BulletCollisionBehaviour {
		None,
		Penetrate,
		Richochet
	}

}