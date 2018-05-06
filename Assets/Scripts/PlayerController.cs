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

	// camera
	int lastZoom = 0;
	GameObject playerCamera;
	CameraController camCon;
	float baseZoom;
	float zoomPerSide = 0.5f;
	float zoomTime = 3f;

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
		camCon = playerCamera.GetComponent<CameraController> ();
		camCon.target = this.gameObject.transform;

		leftTouchArea = playerCamera.transform.Find("LeftTouchArea").gameObject;
		rightTouchArea = playerCamera.transform.Find("RightTouchArea").gameObject;

		baseZoom = camCon.zoomedInSize;
	}
	
	void FixedUpdate () {
		if (isLocalPlayer && pc.alive) {
			var moveInput = new Vector2 (Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).normalized;
			pc.Move (moveInput);

			var	rotateInput = Input.GetAxis("Rotation");
			pc.Rotate (rotateInput);

//			if (Input.GetKeyDown ("=")) {
//				pc.CmdChangeSidesCount (pc.sidesCount + sidesCountIncrement);
//			}
//			if (Input.GetKeyDown ("-")) {
//				pc.CmdChangeSidesCount (pc.sidesCount - sidesCountIncrement);
//			}

			if (Input.GetKeyDown ("0")) { // for testing damage bursts
				pc.TakeDamage (100f, pc.sidesGOArray [0].transform);
			}

			if (Input.GetKeyDown ("9")) { // for testing damage bursts
				pc.HitInWeakSpot();
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

	public void UpdatedPolySides (float newSides) {
		int newZoomInt = Mathf.FloorToInt (newSides);
		if (newZoomInt != lastZoom) {
			float newZoom = (newSides * zoomPerSide) + baseZoom;
			camCon.SetZoom (newZoom, zoomTime);
			lastZoom = newZoomInt;
		}
	}
}
