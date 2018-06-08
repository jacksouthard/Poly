using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerController : NetworkBehaviour {
	// ui
	Animator deathAnimator; 

	// camera
	int lastZoom = 0;
	GameObject playerCamera;
	CameraController camCon;
	float baseZoom;
	float zoomPerSide = 0.5f;
	float zoomTime = 3f;

	// death
	float deathWait = 2f;
	float deathTimer = 0f;

	PolyController pc;

	void Awake () {
		pc = GetComponent<PolyController> ();
	}

	void Start () {
		deathAnimator = GameObject.Find ("Canvas").transform.Find ("GameOver").GetComponent<Animator> ();
		if (isLocalPlayer) {
			GameManager.instance.playerTransform = transform;
			CameraSetup();	
		}

	}

	// CAMERA ---------------------------------------------------------------------------------------------

	void CameraSetup () {			
		// sets up game camera for individual player
		GameObject playerCameraPrefab = Resources.Load("PlayerCamera") as GameObject;
		playerCamera = Instantiate(playerCameraPrefab, new Vector3 (0,0, -10), Quaternion.Euler(0,0,0));
		camCon = playerCamera.GetComponent<CameraController> ();
		camCon.target = this.gameObject.transform;

		baseZoom = camCon.zoomedInSize;
	}
	
	void FixedUpdate () {
		if (isLocalPlayer && pc.alive) {
			var moveInput = new Vector2 (Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).normalized;
			pc.Move (moveInput);

			var	rotateInput = Input.GetAxis("Rotation");
			pc.Rotate (rotateInput);
		}
	}

	void Update () {
		if (!isLocalPlayer) {
			return;
		}

		if (deathTimer > 0f) {
			deathTimer -= Time.deltaTime;
			if (deathTimer <= 0f) {
				StartRespawnAnimation ();
			}
		}

		if (!pc.alive && Input.GetKeyDown(KeyCode.R) && deathTimer <= 0f) {
			// respawn if dead
			deathAnimator.SetTrigger ("Exit");
			playerCamera.GetComponent<CameraController> ().ZoomIn ();
			pc.CmdResetPlayer();
		}

		if (pc.alive) {
			if (Input.GetKeyDown ("0")) { // for testing damage bursts
				pc.TakeDamage (100f, pc.sidesGOArray [0].transform, null);
			}

			if (Input.GetKeyDown ("9")) { // for testing instant death
				pc.HitInWeakSpot (null);
			}

//			if (Input.GetKeyDown ("=")) { // for testing instant death
//				pc.CmdKilledByPlayer (netId.Value);
//			}
		}
	}

	public void PolyDied () {
		playerCamera.GetComponent<CameraController> ().ZoomOut ();
		deathTimer = deathWait;
	}

	void StartRespawnAnimation () {
		deathAnimator.SetTrigger ("Enter");
	}

	public void UpdatedPolySides (float newSides) {
		if (camCon != null) {
			int newZoomInt = Mathf.FloorToInt (newSides);
			if (newZoomInt != lastZoom) {
				float newZoom = (newSides * zoomPerSide) + baseZoom;
				camCon.SetZoom (newZoom, zoomTime);
				lastZoom = newZoomInt;
			}
		}
	}
}
