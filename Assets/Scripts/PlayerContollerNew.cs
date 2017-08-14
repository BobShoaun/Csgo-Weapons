using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerContollerNew : MonoBehaviour
{
	public Text text;
	/**
 * Just some side notes here.
 *
 * - Should keep in mind that idTech's cartisian plane is different to Unity's:
 *    Z axis in idTech is "up/down" but in Unity Z is the local equivalent to
 *    "forward/backward" and Y in Unity is considered "up/down".
 *
 * - Code's mostly ported on a 1 to 1 basis, so some naming convensions are a
 *   bit fucked up right now.
 *
 * - UPS is measured in Unity units, the idTech units DO NOT scale right now.
 *
 * - Default values are accurate and emulates Quake 3's feel with CPM(A) physics.
 */
	public Transform  playerView;  // Must be a camera;  // Must be a camera
	public float playerViewYOffset = 0.6f; // The height at which the camera is bound to
	public float xMouseSensitivity = 30.0f;
	public float yMouseSensitivity = 30.0f;

	/* Frame occuring factors */
	public float gravity  = 20.0f;
	public float friction  = 6f;                // Ground friction

	/* Movement stuff */
	public float moveSpeed = 7.0f;  // Ground move speed
	public float runAcceleration = 14;   // Ground accel
	public float runDeacceleration = 10;   // Deacceleration that occurs when running on the ground
	public float airAcceleration = 2.0f;  // Air accel
	public float airDeacceleration = 2.0f;    // Deacceleration experienced when opposite strafing
	public float airControl = 0.3f;  // How precise air control is
	public float sideStrafeAcceleration = 50;   // How fast acceleration occurs to get up to sideStrafeSpeed when side strafing
	public float sideStrafeSpeed = 1;    // What the max speed to generate when side strafing
	public float jumpSpeed = 8.0f;  // The speed at which the character's up axis gains when hitting jump
	public float moveScale = 1.0f;

	/* print() styles */
	//var style : GUIStyle;

	AudioSource audiosurce;

	/* Sound stuff */
	public AudioClip[] jumpSounds;

	/* FPS Stuff */
	public float fpsDisplayRate = 4.0f;  // 4 updates per sec.

	/* Prefabs */
	public GameObject gibEffectPrefab;

	private int frameCount = 0;
	private float dt = 0.0f;
	private float fps = 0.0f;

	private CharacterController controller;

	// Camera rotationals
	private float rotX = 0.0f;
	private float rotY = 0.0f;

	private Vector3 moveDirection  = Vector3.zero;
	private Vector3 moveDirectionNorm = Vector3.zero;
	private Vector3 playerVelocity = Vector3.zero;
	private float playerTopVelocity = 0.0f;

	//If true then the player is fully on the ground
	private bool grounded = false;

	// Q3: players can queue the next jump just before he hits the ground
	private bool wishJump = false;

	//Used to display real time friction values
	private float playerFriction  = 0.0f;

	//Contains the command the user wishes upon the character
	class Cmd
	{
		public float forwardmove;
		public float rightmove;
		public float upmove;
	}
	private Cmd cmd  ; // Player commands, stores wish commands that the player asks for (Forward, back, jump, etc)

	/* Player statuses */
	private bool isDead = false;

	private Vector3 playerSpawnPos;
	private Quaternion playerSpawnRot;



	void Start()
	{
		audiosurce = GetComponent<AudioSource>();

		/* Hide the cursor */
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
		// Screen.showCursor = false;
		// Screen.lockCursor = true;

		/* Put the camera inside the capsule collider */
		playerView.position = this.transform.position + new Vector3(0, playerViewYOffset,0);
		//playerView.position.y = this.transform.position.y + playerViewYOffset;

		controller = GetComponent<CharacterController>();// CharacterController);
		cmd = new Cmd();

		//Set the spawn position of the player
		playerSpawnPos = transform.position;
		playerSpawnRot = this.playerView.rotation;
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
		}

		/* Do FPS calculation */
		frameCount++;
		dt += Time.deltaTime;
		if (dt > 1.0 / fpsDisplayRate)
		{
			fps = Mathf.Round(frameCount / dt);
			frameCount = 0;
			dt -= 1.0f / fpsDisplayRate;
		}

		/* Ensure that the cursor is locked into the screen */
		if (Cursor.lockState == CursorLockMode.None)
		{
			if (Input.GetMouseButtonDown(0))
				Cursor.lockState = CursorLockMode.Locked;
		}

		/* Camera rotation stuff, mouse controls this shit */
		rotX -= Input.GetAxis("Mouse Y") * xMouseSensitivity * 0.02f;
		rotY += Input.GetAxis("Mouse X") * yMouseSensitivity * 0.02f;

		//Clamp the X rotation
		if (rotX < -90)
			rotX = -90;
		else if (rotX > 90)
			rotX = 90;

		this.transform.rotation = Quaternion.Euler(0, rotY, 0); // Rotates the collider
		playerView.rotation = Quaternion.Euler(rotX, rotY, 0); // Rotates the camera

		//Set the camera's position to the transform
		//playerView.position = this.transform.position;
		//playerView.position.y = this.transform.position.y + playerViewYOffset;

		playerView.position = this.transform.position + new Vector3(0, playerViewYOffset, 0);

		/* Movement, here's the important part */
		QueueJump();
		if (controller.isGrounded)
			GroundMove();
		else
			AirMove();

		//Move the controller
		controller.Move(playerVelocity * Time.deltaTime);

		/* Calculate top velocity */
		Vector3 udp = playerVelocity;
		udp.y = 0.0f;
		if (playerVelocity.magnitude > playerTopVelocity)
			playerTopVelocity = playerVelocity.magnitude;
		
		Vector3 velo = controller.velocity;
		velo.y = 0;
		text.text = "FPS: " + fps
			+ "  Speed: " + Mathf.Round(controller.velocity.magnitude * 100) / 100 + "ups"
			//+ "  Top Speed: " + Mathf.Round(playerTopVelocity * 100) / 100 + "ups"
			+ "  Lateral Speed: " + Mathf.Round (velo.magnitude);

		if (Input.GetKeyUp(KeyCode.X))
			PlayerExplode();
		if (Input.GetButton("Fire1") && isDead)
			PlayerSpawn();
	}

	/*******************************************************************************************************\
|* MOVEMENT
\*******************************************************************************************************/

	/**
	* Sets the movement direction based on player input
	*/
	void SetMovementDir()
	{
		cmd.forwardmove = Input.GetAxisRaw("Vertical");
		cmd.rightmove = Input.GetAxisRaw("Horizontal");
	}

	/**
     * Queues the next jump just like in Q3
     */
	void QueueJump()
	{
		if (Input.GetKeyDown(KeyCode.Space) && !wishJump)
			wishJump = true;
		if (Input.GetKeyUp(KeyCode.Space))
			wishJump = false;
	}

	/**
     * Execs when the player is in the air
     */
	void AirMove()
	{
		Vector3 wishdir;
		float accel;

		float scale = CmdScale();

		SetMovementDir();

		wishdir = new Vector3(cmd.rightmove, 0f, cmd.forwardmove);
		wishdir = transform.TransformDirection(wishdir);

		float wishspeed = wishdir.magnitude;
		wishspeed *= moveSpeed;

		wishdir.Normalize();
		//moveDirectionNorm = wishdir;
		wishspeed *= scale;

		if (Vector3.Dot(playerVelocity, wishdir) < 0)
			accel = airDeacceleration;
		else
			accel = airAcceleration;

		//CPM: Aircontrol
		float wishspeed2 = wishspeed;
		//If the player is ONLY strafing left or right
		if (cmd.forwardmove == 0 && cmd.rightmove != 0)
		{
			if (wishspeed > sideStrafeSpeed)
				wishspeed = sideStrafeSpeed;
			accel = sideStrafeAcceleration;
		}

		Accelerate(wishdir, wishspeed, accel);
		if (airControl > 0)
			AirControl(wishdir, wishspeed2);
		// !CPM: Aircontrol

		//Apply gravity
		playerVelocity.y -= gravity * Time.deltaTime;

		//LEGACY MOVEMENT SEE BOTTOM
	}

	/**
     * Air control occurs when the player is in the air, it allows
     * players to move side to side much faster rather than being
     * 'sluggish' when it comes to cornering.
     */
	void AirControl(Vector3 wishdir, float wishspeed)
	{
		float zspeed;
		float speed;
		float dot;
		float k;
		int i;

		//Can't control movement if not moving forward or backward
		if (cmd.forwardmove == 0 || wishspeed == 0)
			return;

		zspeed = playerVelocity.y;
		playerVelocity.y = 0;
		/* Next two lines are equivalent to idTech's VectorNormalize() */
		speed = playerVelocity.magnitude;
		playerVelocity.Normalize();

		dot = Vector3.Dot(playerVelocity, wishdir);
		k = 32;
		k *= airControl * dot * dot * Time.deltaTime;

		//Change direction while slowing down
		if (dot > 0)
		{
			playerVelocity.x = playerVelocity.x * speed + wishdir.x * k;
			playerVelocity.y = playerVelocity.y * speed + wishdir.y * k;
			playerVelocity.z = playerVelocity.z * speed + wishdir.z * k;

			playerVelocity.Normalize();
			moveDirectionNorm = playerVelocity;
		}

		playerVelocity.x *= speed;
		playerVelocity.y = zspeed; // Note this line
		playerVelocity.z *= speed;

	}

	/**
     * Called every frame when the engine detects that the player is on the ground
     */
	void GroundMove()
	{
		Vector3 wishdir;
		Vector3 wishvel ;

		// Do not apply friction if the player is queueing up the next jump
		if (!wishJump)
			ApplyFriction(1.0f);
		else
			ApplyFriction(0);

		var scale = CmdScale();

		SetMovementDir();

		wishdir = new Vector3(cmd.rightmove, 0, cmd.forwardmove);
		wishdir = transform.TransformDirection(wishdir);
		wishdir.Normalize();
		//moveDirectionNorm = wishdir;

		var wishspeed = wishdir.magnitude;
		wishspeed *= moveSpeed;

		Accelerate(wishdir, wishspeed, runAcceleration);

		// Reset the gravity velocity
		playerVelocity.y = 0;

		if (wishJump)
		{
			playerVelocity.y = jumpSpeed;
			wishJump = false;
			PlayJumpSound();
		}
	}

	/**
     * Applies friction to the player, called in both the air and on the ground
     */
	void ApplyFriction(float t)
	{
		Vector3 velocity  = playerVelocity; // Equivalent to: VectorCopy();
		velocity.y = 0.0f;

		float speed = velocity.magnitude;
		float drop = 0f;

		/* Only if the player is on the ground then apply friction */
		if (controller.isGrounded) {
			float control = speed < runDeacceleration ? runDeacceleration : speed;
			drop = control * friction * Time.deltaTime * t;
		}

		float newspeed = speed - drop;

		playerFriction = newspeed; // visual

		if (newspeed < 0)
			newspeed = 0;
		
		if (speed > 0)
			newspeed /= speed;

		playerVelocity *= newspeed;
	}

	/**
     * Calculates wish acceleration based on player's cmd wishes
     */
	void Accelerate(Vector3 wishdir , float wishspeed, float accel)
	{
		float currentspeed = Vector3.Dot(playerVelocity, wishdir);
		float addspeed = wishspeed - currentspeed;

		if (addspeed <= 0)
			return;
		
		float accelspeed = accel * Time.deltaTime * wishspeed;

		if (accelspeed > addspeed)
			accelspeed = addspeed;

		playerVelocity.x += accelspeed * wishdir.x;
		playerVelocity.z += accelspeed * wishdir.z;
	}




	void LateUpdate()
	{

	}

	void OnGUI()
	{
		
		//GUI.Label(Rect(0, 0, 400, 100), "FPS: " + fps, style);
		//var ups = controller.velocity;
		//ups.y = 0;
		//GUI.Label(Rect(0, 15, 400, 100), "Speed: " + Mathf.Round(ups.magnitude * 100) / 100 + "ups", style);
		//GUI.Label(Rect(0, 30, 400, 100), "Top Speed: " + Mathf.Round(playerTopVelocity * 100) / 100 + "ups", style);
	}

	/*
    ============
    PM_CmdScale
    Returns the scale factor to apply to cmd movements
    This allows the clients to use axial -127 to 127 values for all directions
    without getting a sqrt(2) distortion in speed.
    ============
    */
	float CmdScale()
	{
		float max = Mathf.Abs(cmd.forwardmove);
		if (Mathf.Abs(cmd.rightmove) > max)
			max = Mathf.Abs(cmd.rightmove);
		if (max == 0)
			return 0;

		float total = Mathf.Sqrt(cmd.forwardmove * cmd.forwardmove + cmd.rightmove * cmd.rightmove);
		float scale = moveSpeed * max / (moveScale * total);

		return scale;
	}


	/**
     * Plays a random jump sound
     */
	void PlayJumpSound() {
		return;
		//Don't play a new sound while the last hasn't finished
		if (audiosurce.isPlaying)
			return;
		audiosurce.clip = jumpSounds[Random.Range(0, jumpSounds.Length)];
		audiosurce.Play();
	}

	void PlayerExplode()
	{
		var velocity = controller.velocity;
		velocity.Normalize();
		// var gibEffect = Instantiate(gibEffectPrefab, transform.position, Quaternion.identity);
		// gibEffect.GetComponent(GibFX).Explode(transform.position, velocity, controller.velocity.magnitude);
		isDead = true;
	}

	void PlayerSpawn()
	{
		this.transform.position = playerSpawnPos;
		this.playerView.rotation = playerSpawnRot;
		rotX = 0.0f;
		rotY = 0.0f;
		playerVelocity = Vector3.zero;
		isDead = false;
	}
}



