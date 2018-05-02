using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerController : NetworkBehaviour {
	// controls
	GameObject leftTouchArea;
	GameObject rightTouchArea;
	bool useTouch = true;
	bool useKeyboard = true;

	// ui
	Animator deathAnimator;

	GameObject playerCamera;

	float sidesCountIncrement = 0.5f;

	PolyController pc;

	void Awake () {
		pc = GetComponent<PolyController> ();
	}

	void Start () {
		deathAnimator = GameObject.Find ("Canvas").transform.Find ("GameOver").GetComponent<Animator> ();

	}

	// CAMERA ---------------------------------------------------------------------------------------------

	public override void OnStartLocalPlayer() {
		CameraSetup();	
	}

	void CameraSetup () {			
		// sets up game camera for individual player
		GameObject playerCameraPrefab = Resources.Load("PlayerCamera") as GameObject;
		Instantiate(playerCameraPrefab, new Vector3 (0,0, -10), Quaternion.Euler(0,0,0));

		playerCamera = GameObject.Find("PlayerCamera(Clone)");
		playerCamera.GetComponent<CameraController>().target = this.gameObject.transform;

		leftTouchArea = playerCamera.transform.Find("LeftTouchArea").gameObject;
		rightTouchArea = playerCamera.transform.Find("RightTouchArea").gameObject;
	}
	
	void Update () {
		if (isLocalPlayer && pc.alive) {
			var moveInput = new Vector2 (Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).normalized;
			pc.Move (moveInput);

			var	rotateInput = Input.GetAxis("Rotation");
			pc.Rotate (rotateInput);

			if (Input.GetKeyDown ("=")) {
				pc.CmdChangeSidesCount (pc.sidesCount + sidesCountIncrement);
			}
			if (Input.GetKeyDown ("-")) {
				pc.CmdChangeSidesCount (pc.sidesCount - sidesCountIncrement);
			}

			if (Input.GetKeyDown ("0")) { // for testing damage bursts
				pc.TakeDamage (100f, pc.sidesGOArray [0].transform);
			}

			if (Input.GetKeyDown ("9")) { // for testing damage bursts
				pc.HitInWeakSpot();
			}

			if (Input.GetKeyDown (KeyCode.Y)) { // for testing time scale
				Time.timeScale = 0f;
			}

			if (Input.GetKeyDown (KeyCode.H)) { // for testing time scale
				Time.timeScale = 1f;
			}
		}

		if (!pc.alive && isLocalPlayer && Input.GetKeyDown(KeyCode.R)) {
			// respawn if dead
			deathAnimator.SetTrigger ("Exit");
			playerCamera.GetComponent<CameraController> ().ZoomIn ();
			pc.CmdResetPlayer();
		}
	}

	public void PolyDied () {
		playerCamera.GetComponent<CameraController> ().ZoomOut ();
	}
}
