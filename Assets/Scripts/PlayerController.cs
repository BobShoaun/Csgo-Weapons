using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityRandom = UnityEngine.Random;

[RequireComponent (typeof (CharacterController), typeof (AudioSource))]
public class PlayerController : MonoBehaviour {

	[Header ("Movement")]
	[SerializeField]
	private float walkSpeed = 5;
	[SerializeField]
	private float sprintSpeed = 10;
	[SerializeField]
	private KeyCode sprintButton = KeyCode.LeftShift;
	[SerializeField] [Range (0, 1)]
	private float strafeSpeedPercent = 0.5f;
	[SerializeField] [Range (0, 1)]
	private float backwardSpeedPercent = 0.5f;
	[SerializeField] [Range (0, 1)]
	private float airSpeedPercent = 0.75f;
	[SerializeField]
	private float jumpSpeed = 10;
	[SerializeField]
	private KeyCode jumpButton = KeyCode.Space;
	[SerializeField]
	private float gravityMultiplier = 2;
	[SerializeField]
	private float terminalSpeed = -200; // Max falling speed
	[SerializeField]
	private float speedMaximumDelta = 5; // The maximum amount the speed can change per second
	[Header ("Camera")]
	[SerializeField]
	public Vector2 sensitivity = new Vector2 (3, 3);
	[SerializeField] [Range (-360, 0)]
	private float minimumXAngle = -70;
	[SerializeField] [Range (0, 360)]
	private float maximumXAngle = 70;
	[SerializeField]
	private float cameraSmoothSpeed = 18; // How fast it takes for camera to reach desired rotation with smoothing applied
	[SerializeField] [Range (1, 179)]
	private float stationaryFieldOfView = 60;
	[SerializeField] [Range (1, 179)]
	private float walkFieldOfView = 63;
	[SerializeField] [Range (1, 179)]
	private float sprintFieldOfView = 67;
	[SerializeField]
	private float fieldOfViewMaximumDelta = 5; // The maximum amount the field of view can change per second
	[Header ("View Bobbing")]
	[SerializeField]
	private float walkBobSpeed = 1;
	[SerializeField]
	private float sprintBobSpeed = 1.5f;
	[SerializeField]
	private AnimationCurve bobCurveX = new AnimationCurve (new Keyframe (0, 0), new Keyframe (0.25f, 0.05f), new Keyframe (0.5f, 0, -0.2f, -0.2f), new Keyframe (0.75f, -0.05f), new Keyframe (1, 0));
	[SerializeField]
	private AnimationCurve bobCurveY = new AnimationCurve (new Keyframe (0, 0), new Keyframe (0.25f, 0.15f), new Keyframe (0.5f, 0), new Keyframe (0.75f, 0.15f), new Keyframe (1, 0));
	[SerializeField]
	private float fallImpactBobSpeed = 1;
	[SerializeField]
	private AnimationCurve fallImpactBobCurve = new AnimationCurve (new Keyframe (0, 0), new Keyframe (0.5f, -0.03f), new Keyframe (1, 0));
	[SerializeField]
	private float bobSpeedMaximumDelta = 0.5f; // The maximum amount the bob speed can change per second
	[SerializeField]
	private float bobTransitionBufferDuration = 0.2f; // Time in seconds to wait before view bobbing changes
	[Header ("Audio")]
	[SerializeField]
	private AudioClip[] footSteps;
	[SerializeField]
	private AudioClip jump;
	[SerializeField]
	private AudioClip fallImpact;
	[SerializeField]
	private float summersaultSpeed;

	private CharacterController characterController;
	private AudioSource audioSource;
	[SerializeField]
	private Transform camTransform;
	private Coroutine stateDependantCoroutine;
	private Movement previousMovement;
	private Vector3 velocity;
	private Vector3 viewBobDisplacement;
	private Vector3 fallImpactBobDisplacement;
	private Vector3 originalCameraLocalPosition;
	private Vector2 rotationAngle;
	private float speed;
	private float bobSpeed;
	private float bobPercent;
	private float targetBobSpeed;
	private float targetSpeed;
	private float targetFieldOfView;
	private bool wasGrounded;
	private bool jumping;
	private bool doViewBob;
	private bool playFootStep1 = true, playFootStep2 = true;

	public float fovOffset = 0;
	private float fov = 60;

	private Player player;

	private Movement CurrentMovement {
		set { 
			if (previousMovement != value) {
				if (stateDependantCoroutine != null)
					StopCoroutine (stateDependantCoroutine);
				switch (value) {
				case Movement.Stationary:
					stateDependantCoroutine = StartCoroutine (DelayedInvoke (() => {
						bobPercent = 0;
						playFootStep1 = true;
						playFootStep2 = true;
					}, bobTransitionBufferDuration));
					targetBobSpeed = 0;
					doViewBob = false;
					targetSpeed = 0;
					targetFieldOfView = stationaryFieldOfView;
					break;
				case Movement.Walking:
					if (previousMovement == Movement.Stationary)
						stateDependantCoroutine = StartCoroutine (DelayedInvoke (() => doViewBob = true, bobTransitionBufferDuration));
					else
						doViewBob = true;
					targetBobSpeed = walkBobSpeed;
					targetSpeed = walkSpeed;
					targetFieldOfView = walkFieldOfView;
					break;
				case Movement.Sprinting:
					if (previousMovement == Movement.Stationary)
						stateDependantCoroutine = StartCoroutine (DelayedInvoke (() => doViewBob = true, bobTransitionBufferDuration));
					else
						doViewBob = true;
					targetBobSpeed = sprintBobSpeed;
					targetSpeed = sprintSpeed;
					targetFieldOfView = sprintFieldOfView;
					break;
				case Movement.Air:
					if (previousMovement == Movement.Sprinting) {
						targetSpeed = sprintSpeed * airSpeedPercent;
						targetFieldOfView = sprintFieldOfView;
					} else {
						targetSpeed = walkSpeed * airSpeedPercent;
						targetFieldOfView = walkFieldOfView;
					}
					targetBobSpeed = 1;
					doViewBob = false;
					bobPercent = 0;
					playFootStep1 = false;
					playFootStep2 = true;
					break;
				}
			}
			previousMovement = value;
		}
	}

	private enum Movement { Stationary, Walking, Sprinting, Air }

	private void Awake () {
		characterController = GetComponent<CharacterController> ();
		audioSource = GetComponent<AudioSource> ();
		//camTransform = GetComponentInChildren<Camera> ();
		viewBobDisplacement = originalCameraLocalPosition = camTransform.transform.localPosition;
		player = GetComponent<Player> ();
	}

	private void Update () {
		//if (Input.GetKeyDown (KeyCode.G))
		//	StartCoroutine (Summersault ());
		speed = Mathf.MoveTowards (speed, targetSpeed, Time.deltaTime * speedMaximumDelta); // Change speed towards target speed
		bobSpeed = Mathf.MoveTowards (bobSpeed, targetBobSpeed, Time.deltaTime * bobSpeedMaximumDelta); // Change bob speed towards target bob speed
		fov = Mathf.MoveTowards (fov, targetFieldOfView, Time.deltaTime * fieldOfViewMaximumDelta); // Change field of view towards target field of view
		//camTransform.fieldOfView = fov + fovOffset;
		if (doViewBob)
			ViewBob ();
		else // Return to original camera position if not view bobbing
			viewBobDisplacement = Vector3.MoveTowards (viewBobDisplacement, originalCameraLocalPosition, Time.deltaTime * bobTransitionBufferDuration);
		RaycastHit raycastHit;
		float finalSpeed = speed; 
		if (!characterController.isGrounded)
			CurrentMovement = Movement.Air; // Considered on air as long as controller is not grounded
		else if (Mathf.Abs (Input.GetAxis ("Horizontal")) + Mathf.Abs (Input.GetAxis ("Vertical")) < 0.01f)
			CurrentMovement = Movement.Stationary; // Considered stationary when no input is given
		else if ((characterController.collisionFlags & CollisionFlags.CollidedSides) == CollisionFlags.CollidedSides && Physics.Raycast (transform.position, transform.forward, out raycastHit, 1)) {
			CurrentMovement = raycastHit.distance < 0.7f ? Movement.Stationary : Input.GetKey (sprintButton) ? Movement.Sprinting : Movement.Walking; // Allow movement when controller side is collided with wall but is not directly facing the wall
			finalSpeed = speed * raycastHit.distance * 0.75f; // Apply movement penalty when rubbing against wall
		} else
			CurrentMovement = Input.GetKey (sprintButton) ? Movement.Sprinting : Movement.Walking;
		Look ();
		Move (finalSpeed);
	}

	private void OnControllerColliderHit (ControllerColliderHit controllerColliderHit) {
		if (characterController.collisionFlags == CollisionFlags.CollidedBelow)
			return; // Do nothing if standing above collided object
		if (controllerColliderHit.rigidbody && 
			controllerColliderHit.rigidbody.GetComponent<NetworkIdentity> () && 
			!controllerColliderHit.rigidbody.isKinematic)
			player.CmdPushRigidBody (controllerColliderHit.gameObject, 
				controllerColliderHit.moveDirection * velocity.magnitude / 4, 
				controllerColliderHit.point + Vector3.up * controllerColliderHit.point.y);
		//else
		//	RpcPushRigidBody (controllerColliderHit.moveDirection, controllerColliderHit.point);
			
//		Rigidbody collidedRigidbody = controllerColliderHit.rigidbody;
//		if (collidedRigidbody != null && !collidedRigidbody.isKinematic) // Artificially pushes non kinematic rigidbodies, character controller does not work with physics engine
//			collidedRigidbody.AddForceAtPosition (controllerColliderHit.moveDirection * velocity.magnitude / 4, controllerColliderHit.point + Vector3.up * controllerColliderHit.point.y, ForceMode.Impulse); 
    }

	private void Move (float speed) {
		velocity.x = Input.GetAxis ("Horizontal") * speed * strafeSpeedPercent; // Factor in strafe speed percent to horizontal movement
		velocity.z = Input.GetAxis ("Vertical") * speed * (Input.GetAxis ("Vertical") < 0 ? backwardSpeedPercent : 1); // Factor in backward speed percent if vertical movement is negative
		if (characterController.isGrounded) {
			if (!wasGrounded) {
				jumping = false;
				StartCoroutine (FallImpactBob (velocity.y)); // Plays fall impact bob coroutine when controller was not grounded the last frame but is now
				audioSource.PlayOneShot (fallImpact);
				if (velocity.y < -15) {
					StartCoroutine (Summersault ());
				}
			}
			RaycastHit raycastHit; // Sets downward force when ground is detected within the distance of the step offset, this prevents controller from doing mini falls while going down slopes or stairs
			velocity.y = Physics.SphereCast (transform.position, characterController.radius * 0.75f, Vector3.down, out raycastHit, characterController.height / 2 + characterController.stepOffset)
			&& raycastHit.distance <= characterController.height / 2 + characterController.stepOffset + 0.1f && !jumping ? -characterController.stepOffset / Time.deltaTime : 0;
			if (Input.GetKeyDown (jumpButton)) {
				jumping = true;
				velocity.y = jumpSpeed; // Jump
				audioSource.Stop ();
				audioSource.PlayOneShot (jump);
			}
		} else {
			if (wasGrounded && !jumping)
				velocity.y = 0; // Resets gravity when the controller is not grounded but was grounded last frame
			velocity.y = Mathf.Max (velocity.y + Physics.gravity.y * gravityMultiplier * Time.deltaTime, terminalSpeed); // Adds gravity to velocity every frame when not grounded to simulate acceleration until terminal velocity is reached
		}
		wasGrounded = characterController.isGrounded;
		velocity = Vector3.ClampMagnitude (velocity, Mathf.Max (Mathf.Abs (velocity.x), Mathf.Abs (velocity.z)) + Mathf.Abs (velocity.y)); // Clamp diagonal movement based on the largest non diagonal movement ignoring up and down movements
		velocity = transform.TransformDirection (velocity); // Converts the velocity from local space to world space
		characterController.Move (velocity * Time.deltaTime); // Factor in delta time for frame rate independence
	}

	private void Look () {
		rotationAngle.x += Input.GetAxis ("Mouse Y") * -sensitivity.x;
		rotationAngle.x = Mathf.Clamp (rotationAngle.x, minimumXAngle, maximumXAngle); // Clamp up down rotation
		rotationAngle.y += Input.GetAxis ("Mouse X") * sensitivity.y;
		transform.localRotation = Quaternion.AngleAxis (rotationAngle.y, Vector3.up); // Apply horizontal mouse rotation to controller with smoothing
		camTransform.localRotation = Quaternion.AngleAxis (rotationAngle.x, Vector3.right); // Apply vertical mouse rotation to camera with smoothing
		camTransform.localPosition = viewBobDisplacement + fallImpactBobDisplacement; // Apply change in camera local position caused by the view bob and fall impact bob
	}

	private void ViewBob () {
		if (bobPercent > 0 && playFootStep1) {
			playFootStep1 = false;
			//PlayFootStepAudio ();
		} else 
			if (bobPercent > 0.5f && playFootStep2) {
				playFootStep2 = false;
				//PlayFootStepAudio ();
			}
		if (bobPercent > 1) {
			bobPercent = 0;
			playFootStep1 = true;
			playFootStep2 = true;
		} else
			bobPercent += Time.deltaTime * bobSpeed;
		viewBobDisplacement.x = originalCameraLocalPosition.x + bobCurveX.Evaluate (bobPercent);
		viewBobDisplacement.y = originalCameraLocalPosition.y + bobCurveY.Evaluate (bobPercent);
	}

	private IEnumerator FallImpactBob (float fallImpactSpeed) {
		for (float percent = 0; percent <= 1; percent += Time.deltaTime * fallImpactBobSpeed * 30 / -fallImpactSpeed) {
			fallImpactBobDisplacement.y = fallImpactBobCurve.Evaluate (percent) * -fallImpactSpeed;
			yield return null;
		}
		fallImpactBobDisplacement.y = fallImpactBobCurve.Evaluate (1) * -fallImpactSpeed;
	}

	private void PlayFootStepAudio () {
		int clipToPlayIndex = UnityRandom.Range (1, footSteps.Length); // Plays random clip in the array excluding clip with index 0
		audioSource.clip = footSteps [clipToPlayIndex];
		audioSource.Play ();
		footSteps [clipToPlayIndex] = footSteps [0]; 
		footSteps [0] = audioSource.clip; // Sets played clip to index zero so it wont get played twice in a row
	}

	private IEnumerator DelayedInvoke (Action method, float delay) {
		yield return new WaitForSeconds (delay);
		method ();
	}

	private IEnumerator Summersault () {
		for (float percent = 0; percent <= 1; percent += Time.deltaTime * summersaultSpeed) {
			camTransform.transform.localRotation = Quaternion.Euler (360 * percent, 0, 0);
			yield return null;
		}
	}

}