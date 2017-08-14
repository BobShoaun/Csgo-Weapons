using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMotor : MonoBehaviour {

	private const float SourceToUnity = 0.01905f;
	private const float UnityToSource = 1f / SourceToUnity;

	public float speed = 7;
	public float acceleration = 7;
	public float airAcceleration = 7;
	public float friction = 6;
	public float jumpSpeed = 6;
	public float gravity = 800;
	public Vector2 mouseSensitivity = Vector2.one * 5;
	private Vector2 rotation;
	private CharacterController characterController;
	private new Camera camera;

	private Vector3 velocity;
	private bool jump;

	public Text text;

	void Start () {
		speed *= SourceToUnity;
		acceleration *= SourceToUnity;
		airAcceleration *= SourceToUnity;
		friction *= SourceToUnity;
		jumpSpeed *= SourceToUnity;
		gravity *= SourceToUnity;

		characterController = GetComponent<CharacterController> ();
		camera = GetComponentInChildren<Camera> ();
	}

	void Update () {
		Look ();

		Move2 ();
		Vector3 velo = characterController.velocity;
		velo.y = 0;
		text.text = "Speed : " + Mathf.Round (characterController.velocity.magnitude * UnityToSource)
		+ " Lateral Speed: " + Mathf.Round (velo.magnitude * UnityToSource)
			+ " Grounded : " + characterController.isGrounded;
	}

	void Move () {
		if (Input.GetKeyDown (KeyCode.Space))
			jump = !jump;
		
		if (characterController.isGrounded) {
			GroundMove ();
			if (jump) {
				jump = false;
				velocity.y = jumpSpeed;
			}

		} 
		else {
			velocity.y -= 20 * Time.deltaTime;
			//AirMove ();
		}

		characterController.Move (velocity * Time.deltaTime);
	}

	void GroundMove () {
		//ApplyFriction (1);
		Vector3 direction = new Vector3 (Input.GetAxis ("Horizontal"), 0, Input.GetAxis ("Vertical")).normalized;
		direction = transform.TransformDirection (direction);
		//velocity = moveDirection * speed;
		//Accelerate (direction, speed, acceleration);

		float speed = velocity.magnitude;
		if (speed != 0) // To avoid divide by zero errors
		{
			float drop = speed * friction * Time.deltaTime;
			velocity *= Mathf.Max(speed - drop, 0) / speed; // Scale the velocity based on friction.
		}

		velocity = Accelerate2 (direction, velocity, acceleration, 7);
	}

	void AirMove () {
		Vector3 direction = new Vector3 (Input.GetAxis ("Horizontal"), 0, Input.GetAxis ("Vertical")).normalized;
		direction = transform.TransformDirection (direction);
		float accel;
		float mSpeed = speed;
		if (Vector3.Dot(velocity, direction) < 0)
			accel = 0;
		else
			accel = acceleration;
		
		if (Input.GetAxis ("Vertical") == 0 && Input.GetAxis ("Horizontal") != 0) {
			if (mSpeed > 7)
				mSpeed = 7;
			accel = 10;
		}
		Accelerate (direction, mSpeed, accel);
		velocity.y -= 20 * Time.deltaTime;
	}
		
	void Look () {
		rotation.x -= Input.GetAxis ("Mouse Y") * mouseSensitivity.y;
		rotation.y += Input.GetAxis ("Mouse X") * mouseSensitivity.x;
		rotation.x = Mathf.Clamp (rotation.x, -90, 90);
		transform.rotation = Quaternion.AngleAxis (rotation.y, Vector3.up);
		camera.transform.localRotation = Quaternion.AngleAxis (rotation.x, Vector3.right);
	}

	void Jump () {
		
	}

	void Accelerate (Vector3 direction, float speed, float acceleration) {
		float currentSpeed = Vector3.Dot (velocity, direction);
		float addSpeed = speed - currentSpeed;

		if (addSpeed <= 0)
			return;

		float resultingSpeed = acceleration * Time.deltaTime * speed;

		if (resultingSpeed > addSpeed)
			resultingSpeed = addSpeed;

		velocity.x += resultingSpeed * direction.x;
		velocity.z += resultingSpeed * direction.z;
	}

	void ApplyFriction (float amount) {
		Vector3 vel  = velocity;
		vel.y = 0;

		float speed = vel.magnitude;
		float drop = 0f;

		/* Only if the player is on the ground then apply friction */
		if (characterController.isGrounded) {
			float control = speed < acceleration ? acceleration : speed;
			drop = control * friction * Time.deltaTime * amount;
		}

		float newspeed = speed - drop;
		if (newspeed < 0)
			newspeed = 0;

		if (speed > 0)
			newspeed /= speed;

		velocity *= newspeed;
	}

	private void Move2 () {
		Vector3 direction = new Vector3 (Input.GetAxis ("Horizontal"), 0, Input.GetAxis ("Vertical")).normalized;
		direction = transform.TransformDirection (direction);

		if (Input.GetKeyDown (KeyCode.Space))
			jump = !jump;

		if (characterController.isGrounded) {
			velocity = MoveGround (direction, velocity);
			if (jump) {
				jump = false;
				velocity.y = jumpSpeed;
			}

		} 
		else {
			velocity = MoveAir (direction, velocity);
			//velocity = Vector3.ClampMagnitude (velocity, speed);
			//velocity.y -= gravity * Time.deltaTime;
			//AirMove ();
		}

		characterController.Move (velocity * Time.deltaTime);
	}

	private Vector3 MoveGround (Vector3 direction, Vector3 previousVelocity) {
		// Apply Friction
		float speed2 = previousVelocity.magnitude;
		if (speed2 != 0) {
			float drop = speed2 * friction * Time.fixedDeltaTime;
			previousVelocity *= Mathf.Max (speed2 - drop, 0) / speed2; // Scale the velocity based on friction.
		}

		// ground_accelerate and max_velocity_ground are server-defined movement variables
		return Accelerate2 (direction, previousVelocity, acceleration, speed);
	}

	private Vector3 MoveAir (Vector3 direction, Vector3 previousVelocity) {
		Vector3 acce = Accelerate2 (direction, previousVelocity, airAcceleration, speed);
		acce.y -= gravity * Time.deltaTime;
		return acce;
	}

	private Vector3 Accelerate2 (Vector3 direction, Vector3 previousVelocity, float acceleration, float maxSpeed) {
		float projectedVelocity = Vector3.Dot (previousVelocity, direction);
		//float acceleratedVelocity = acceleration * Time.deltaTime;

		//if (projectedVelocity + acceleratedVelocity > maxVelocity)
		//	acceleratedVelocity = maxVelocity - projectedVelocity;
		
		//return previousVelocity + direction * acceleratedVelocity;

		float totalSpeedComponent = projectedVelocity + acceleration * Time.deltaTime;
		// is it small enough?
		if (totalSpeedComponent < maxSpeed)
		{
			return previousVelocity + (direction * acceleration * Time.deltaTime);
			//nextSpeed = currentSpeed.add(accelDir.times(A*dt));
		} else { // no! it's too big! We must truncate -_-'
			return previousVelocity + (direction * (maxSpeed - projectedVelocity));
			//nextSpeed = currentSpeed.add(accelDir.times(V - vcosdelta));
		}
	}

}