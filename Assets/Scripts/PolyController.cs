using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class PolyController : NetworkBehaviour {

	// controls
	GameObject leftTouchArea;
	GameObject rightTouchArea;
	bool useTouch = true;
	bool useKeyboard = true;

	// movement
	float speed = 10f;
	float maxVelocity = 2f;
	float rotationSpeed = 5f;

	// sides
	float damageToSidesRatio = 0.01f;
	float startingSides = 2.2f;
	float sidesCountMin = 2f;
	int sidesCountMax = 12;
	float sidesCountIncrement = 0.5f;

	// side prefabs
	GameObject[] sidesGOArray;
	public GameObject sidePrefab;
	public GameObject sidesContainer;

	// for optimizing frequent GetComponents
	Rigidbody2D rb;
	CircleCollider2D attractZone;
	MeshFilter mf;
	MeshRenderer mr;
	PolygonCollider2D pc;

	// visuals
	public Material fillMaterial;
	private Color[] playerColors = new Color[] {
		new Color(0.30f, 0.63f, 0.73f),
		new Color(0.85f, 0.63f, 0.28f),
		new Color(0.77f, 0.32f, 0.38f),
		new Color(0.59f, 0.45f, 0.82f),
		new Color(0.33f, 0.72f, 0.33f)
	};

	// misc
	public bool alive = true; // only matters locally
	GameObject playerCamera;

	// STARTING ---------------------------------------------------------------------------------------

	void Awake () {
		SetupRendering();
	}

	void Start() {
		// set up references
		rb = GetComponent<Rigidbody2D>();
		attractZone = GetComponent<CircleCollider2D> ();

		if (isLocalPlayer) {
			CmdChangeSidesCount (startingSides);
			CmdChangePlayerNumber (Random.Range (0, 4));
		} else {
			SetSidesCount (sidesCount);
			ExpressPartData (partData);
		}
	}

	// NETWORK SYNCING ---------------------------------------------------------------------------

	// SidesCount network syncing
	[SyncVar(hook="SetSidesCount")]
	private float sidesCount;

	[SyncVar(hook="SetPlayerNumber")]
	private int playerNumber;

	[Command]
	void CmdChangeSidesCount (float newValue) {
		if (newValue < sidesCountMin) {
			CmdPlayerDie ();
			return;
		}

		// if poly isnt dying...
		sidesCount = Mathf.Clamp (newValue, sidesCountMin, sidesCountMax);
	}

	public void TakeDamage (float damage) {
		CmdChangeSidesCount (sidesCount - (damage * damageToSidesRatio));
 	}

	void SetSidesCount (float newValue)
	{
		if (newValue < sidesCountMin) { // for case where sides == 0 on start
			newValue = sidesCountMin;
		}
		sidesCount = newValue;

		UpdateRendering();
		attractZone.radius = radius + 0.75f;
	}

	// Tell server version of object to change player number/color
	[Command]
	void CmdChangePlayerNumber(int newValue) {
		SetPlayerNumber(newValue);
	}

	// recieves server synced value and updates object locally
	void SetPlayerNumber (int newValue) {
		if (playerNumber == newValue) {
			return;
		}
		playerNumber = newValue;
		UpdateRendering();
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

	// INPUT ------------------------------------------------------------------------------------------------------------
	void Update ()
	{
		if (isLocalPlayer && alive) {
			Move ();
			Rotate ();

			if (Input.GetKeyDown("=")) {
				CmdChangeSidesCount(sidesCount + sidesCountIncrement);
			}
			if (Input.GetKeyDown("-")) {
				CmdChangeSidesCount(sidesCount - sidesCountIncrement);
			}
		}

		if (Input.GetKeyDown (KeyCode.G)) {
			print (partData);
		}

		if (!alive && isLocalPlayer && Input.GetKeyDown (KeyCode.R)) {
			// respawn if dead
			CmdResetPlayer();
		}
	}

	// MOVEMENT AND ROTATION ------------------------------------------------------------------------------------

	void Move ()
	{
		var moveOffset = Vector2.zero;

		// faking touch for now with mouse controls
		if (useTouch) {
			moveOffset = rightTouchArea.GetComponentInChildren<TouchAreaController> ().offset; // TODO optimize later when considering mobile
		}

		if (useKeyboard && moveOffset == Vector2.zero) {
			moveOffset = new Vector2 (Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
		}

		// add force to poly
		if (moveOffset != Vector2.zero) {
			rb.AddForce (moveOffset * speed);
		}

		// limit velocity
		rb.velocity = Vector2.ClampMagnitude(rb.velocity, maxVelocity);
	}

	void Rotate ()
	{
		var rotateOffset = Vector2.zero;

		if (useTouch) {
			rotateOffset = leftTouchArea.GetComponentInChildren<TouchAreaController> ().offset;
		}

		if (useKeyboard && rotateOffset == Vector2.zero) {
			rotateOffset = new Vector2 (0, Input.GetAxis("Rotation"));
		}

		if (rotateOffset != Vector2.zero) {
			rb.AddTorque(rotateOffset.y * rotationSpeed);
		}
	}

	// DEATH AND RESPAWNING ------------------------------------------------------------------------------------------------------------

	[Command]
	void CmdPlayerDie () {
		partData = "------------------------";
		RpcPlayerDie ();
	}

	[ClientRpc]
	void RpcPlayerDie () {
		alive = false;

		// disable visuals
		GetComponent<MeshRenderer> ().enabled = false;
		foreach (var spriteRenderer in GetComponentsInChildren<SpriteRenderer>()) {
			spriteRenderer.enabled = false;
		}
	}

	[Command]
	void CmdResetPlayer () {
		CmdChangeSidesCount(startingSides);
		RpcResetPlayer ();
	}

	[ClientRpc]
	void RpcResetPlayer () {
		// local player (sometimes client) has authority over its own movement
		if (isLocalPlayer) {
			transform.position = Vector3.zero;
			GetComponent<Rigidbody2D> ().velocity = Vector2.zero;
			GetComponent<Rigidbody2D> ().angularVelocity = 0f;
		}

		alive = true;

		// renable visuals
		GetComponent<MeshRenderer> ().enabled = true;
		foreach (var spriteRenderer in GetComponentsInChildren<SpriteRenderer>()) {
			spriteRenderer.enabled = true;
		}
	}

	// DETECTION ----------------------------------------------------------------------------------------

	// Handle a collectable segment entering collect zone around poly
	void OnTriggerEnter2D (Collider2D other) {
		if (other.CompareTag("Collectable")) {
			if (isLocalPlayer) {
				other.gameObject.GetComponent<SegmentController> ().StartTracking (gameObject.transform, false);
			} else {
				other.gameObject.GetComponent<SegmentController> ().StartTracking (gameObject.transform, true);
			}
		}
	}

	[Command]
	public void CmdSegmentStartDestory (GameObject segment) {
		Destroy (segment);
		SegmentsManager.instance.SegmentDestoryed ();
		Collect(0.2f);
	}

	void Collect (float sidesIncrement) {
		CmdChangeSidesCount (sidesCount + sidesIncrement);
	}
		
	// PARTS --------------------------------------------------------------------------------------------------------------------------------

	[SyncVar(hook="ExpressPartData")]
	string partData = "------------------------"; // each set of 00's represents the part data of one side

	public void AttachPartRequest (GameObject part, GameObject side) {
		CmdPartStartDestroy (part.GetComponent<NetworkIdentity>().netId, int.Parse(side.name));
	}

	[Command]
	void CmdPartStartDestroy (NetworkInstanceId partNetID, int sideIndex) {
		GameObject part = NetworkServer.FindLocalObject (partNetID);
		CmdAlterPartInData (part.GetComponent<PartController> ().id, sideIndex);
		Destroy (part);
		PartsManager.instance.PartDestoryed ();
	}

	[Command]
	void CmdDestroyPart (int sideIndex) {
		CmdAlterPartInData (-1, sideIndex); // -1 is code for --
	}

	public void DestroyPartRequest (GameObject side) {
		if (isLocalPlayer) {
			CmdDestroyPart (int.Parse (side.name));
		}
	}

	[Command]
	void CmdRelaySpawnDetatchedPart (int partID, Vector3 spawnPos, Quaternion spawnRot) {
		PartsManager.instance.SpawnDetachedPart (partID, spawnPos, spawnRot);
	}

	[Command]
	void CmdAlterPartInData (int partID, int sideIndex) {
		string IDString = "";

		if (partID != -1) { // -1 code for replace with -- (no part)
			if (partID < 10) {
				IDString = "0" + partID.ToString ();
			} else {
				IDString = partID.ToString ();
			}
		} else {
			IDString = "--";
		}
//		string newData = partData.Remove (sideIndex, 2).Insert(sideIndex, IDString);
		int partSetIndex = sideIndex * 2;
		string before = partData.Substring(0, partSetIndex);
		string after = partData.Substring (partSetIndex + 2);
		string newData = before + IDString + after;
//		print ("O: " + partData + " N: " + newData);
		partData = newData;
	}

	void ExpressPartData (string newPartData) {
		int[] sidePartIDs = new int[sidesCountMax];
		for (int i = 0; i < sidePartIDs.Length; i++) {
			string strPartID = newPartData.Substring (i * 2, 2);
			if (strPartID == "--") {
				sidePartIDs [i] = -1; // code for no part
			} else {
				sidePartIDs [i] = int.Parse (strPartID);
			}
		}
		print ("Expressing: " + newPartData);
		for (int i = 0; i < sidesGOArray.Length; i++) {
			if (sidePartIDs [i] != -1) {
				if (sidesGOArray [i].transform.childCount == 0) {
					// spawnpart on empty side
					PartData data = PartsManager.instance.GetDataWithID (sidePartIDs [i]);
					GameObject newPart = Instantiate (data.prefab, sidesGOArray [i].transform);
					newPart.name = data.prefab.name;
					newPart.transform.localPosition = Vector3.zero;
					newPart.transform.localRotation = Quaternion.identity; 
				}
			} else {
				for (int c = 0; c < sidesGOArray[i].transform.childCount; c++) {
					Destroy (sidesGOArray [i].transform.GetChild (c).gameObject);
				}
			}
		}
	}

	[Command]
	void CmdRelayManualBackupExpressPartData () {
		RpcManualBackupExpressPartData ();
	}

	[ClientRpc]
	void RpcManualBackupExpressPartData () {
		ExpressPartData (partData);
	}

	public void RelayDestoryProjectile (GameObject projectile) {
		if (isLocalPlayer) {
			CmdDestoryProjectile (projectile.GetComponent<NetworkIdentity> ().netId);
		}
	}

	[Command]
	void CmdDestoryProjectile (NetworkInstanceId netID) {
		Destroy (NetworkServer.FindLocalObject (netID));
	}

	public void RelayProjectileSpawn (int projectileIndex, Vector3 spawnPos, Quaternion spawnRot) {
		if (isLocalPlayer) {
			CmdRelayProjectileSpawn (projectileIndex, spawnPos, spawnRot);
		}
	}

	[Command]
	public void CmdRelayProjectileSpawn (int projectileIndex, Vector3 spawnPos, Quaternion spawnRot) {
		PartsManager.instance.CmdSpawnProjectile (projectileIndex, spawnPos, spawnRot);
	}

	// RENDERING --------------------------------------------------------------------------------------------------------------

	float[] angles;
	float radius;

	void SetupRendering ()
	{
		mf = gameObject.AddComponent (typeof(MeshFilter)) as MeshFilter;

		mr = gameObject.AddComponent (typeof(MeshRenderer)) as MeshRenderer;
		mr.material = fillMaterial;

		pc = gameObject.AddComponent (typeof(PolygonCollider2D)) as PolygonCollider2D;
		SetupSides();
	}

	// Setup sides
	void SetupSides() {

		// setup pool of sides in the sidesContainer
		sidesGOArray = new GameObject[sidesCountMax];

		for (var i = 0; i < sidesCountMax; i++) {
			// Create game object
			var newSide = Instantiate(sidePrefab, Vector3.zero, Quaternion.identity) as GameObject;
			newSide.transform.parent = sidesContainer.transform;
			newSide.SetActive(false);
			newSide.name = i.ToString();
			sidesGOArray[i] = newSide;
		}

	}
		
	// Update mesh and polygon collider, and sides geometry
	void UpdateRendering ()
	{
		float[] angles = CalculateAngles (sidesCount);
		int anglesCount = angles.Length;
		int vertexCount = anglesCount + 1;  //includes center
		//for every angle there is own vertex; along the unit circle

		// assume that difference between first and second angles is otherAngle
		radius = PolyRadiusFromSidesCount (angles [1] - angles [0]);

		// Setup temporary arrays
		var vertices = new Vector3[vertexCount]; 
		var uv = new Vector2[vertexCount];

		// Create a vertex for center of polygon
		var center = new Vector3 (0, 0, 0);
		var centerIndex = 0;
		vertices [centerIndex] = center; 
		uv [centerIndex] = new Vector2 (0, 0);

		// For each angle, create a vertex and matching UV texture mapping information
		// V1-Vn are the outside vertices in clockwise order, V1 is top at 0 degrees
		for (var i = 0; i < anglesCount; i++) {
			var angle = angles [i];

			// Vertex
			var x = Mathf.Cos (angle * Mathf.Deg2Rad) * radius;
			var y = Mathf.Sin (angle * Mathf.Deg2Rad) * radius;
			vertices [i + 1] = new Vector3 (x, y, 0);

			// UV: Read about texture mapping here: https://en.wikipedia.org/wiki/UV_mapping
			var u = Mathf.Cos (angle * Mathf.Deg2Rad); // Does this work?
			var v = Mathf.Sin (angle * Mathf.Deg2Rad); // Does this work?
			uv [i + 1] = new Vector2 (u, v);

		}

		// For each vertex, not including center (V0), create a triangle in
		// the mesh from center to next vertex to it (counterclockwise)
		var triangles = new int[anglesCount * 3];
		// iterate through vertices
		for (var i = 0; i < anglesCount; i++) {

			triangles [i * 3 + 0] = centerIndex;
			triangles [i * 3 + 1] = (i == anglesCount - 1) ? 1 : i + 2; // in last case, use 0 degrees (V1);
			triangles [i * 3 + 2] = i + 1;

		}

		// UPDATE MESH and FILTER
		Mesh m = new Mesh ();
		m.vertices = vertices;
		m.uv = uv;
		m.triangles = triangles;
		m.RecalculateBounds ();
		m.RecalculateNormals ();

		mf.mesh = m;

		mr.material.color = playerColors[playerNumber];

		// UPDATE POLYGON COLLIDER
		// the points are all the verticies, minus V0 (center vertex of mesh)

		// build array of points for poly collider
		var points = new Vector2[anglesCount];
		for (var i = 0; i < anglesCount; i++) {
			var vertex = vertices [i + 1];
			points [i] = new Vector2 (vertex.x, vertex.y);
		}

		// set path
		pc.SetPath (0, points);

		// POSITION SIDE PREFABS

		// generate side midpoint angles and distances
		// these will be registration points for borders (sides)
		var midpointAngles = new float[anglesCount];
		var midpointDistances = new float[anglesCount];

		for (var i = 0; i < anglesCount; i++) {
			var a = angles [i];
			// if last angle, use first angle; otherwise use next angle
			var b = (i == anglesCount - 1) ? angles [0] : angles [i + 1]; 

			// if b is less than a then add 360 to b
			if (a > b) b += 360;

			// store what angle the side midpoint is at
			midpointAngles[i] = (a + b) / 2;
			// store distance from center
			midpointDistances[i] = DistanceFromCenterForSideWithArcAngle(b-a);
		}

		// activate and place sides
		for (var i = 0; i < sidesCountMax; i++) {
			var sideGO = sidesGOArray [i];
			if (i+1 < anglesCount) {
				// make active
				sideGO.SetActive (true);
				// set transform
				var angle = midpointAngles[i];
				var distance = midpointDistances[i];
				var x = Mathf.Cos(angle * Mathf.Deg2Rad) * distance;
				var y = Mathf.Sin(angle * Mathf.Deg2Rad) * distance;
				sideGO.transform.localPosition = new Vector3 (x, y, 0);
				sideGO.transform.localRotation = Quaternion.Euler (0, 0, angle-90);
			} else {
				if (sideGO.activeInHierarchy) {
					//				// make inactive, but first check if it has an attached part
					if (isLocalPlayer && sideGO.transform.childCount > 0) {
						// if part is not already detaching
						if (!sideGO.GetComponentInChildren<Part> ().detaching) {
							// spawn detached part
							sideGO.GetComponentInChildren<Part> ().detaching = true;
							int detachedID = PartsManager.instance.GetIDFromName (sideGO.transform.GetChild (0).name);
							CmdRelaySpawnDetatchedPart (detachedID, sideGO.transform.position, sideGO.transform.rotation);

							// destory part on poly
							CmdDestroyPart (i);
							CmdRelayManualBackupExpressPartData ();
						}
					}
					sideGO.SetActive (false);
				}
			}
		}
	}

	// returns angles of all verticies like [0, 120, 240] for triangle
	float[] CalculateAngles (float sides)
	{
		int vertices = Mathf.CeilToInt(sides);

		// special case when shape is complete like sides = 4.0
		bool isEqualSides = (sides == vertices);

		// for sides = 3.1, grow = .1 and grow is the growing side
		float grow = sides - Mathf.Floor (sides);
		float growAngleMax = 360f / sides;
		float growAngle = growAngleMax * grow;
		float anglesOffset = 0f;
		//		float anglesOffset = growAngle / 2;
		//		float anglesOffset = growAngle / 2 + ((Mathf.Floor(sides) % 2) * 180);

		// gives all other angles rest of the circle
		float otherAnglesTotal = 360f - growAngle;
		int otherVerticesCount = vertices;
		if (!isEqualSides) {
			otherVerticesCount -= 1;
		}
		//		print (otherVerticesCount);

		float otherAngle = otherAnglesTotal / otherVerticesCount;

		// builds array of angles to construct polygon
		var angles = new float[vertices];

		for (var i = 0; i < otherVerticesCount; i++) {
			angles [i] = (i * otherAngle) + anglesOffset;
		}

		if (!isEqualSides) {
			angles[otherVerticesCount] = (360f - growAngle) + anglesOffset;
		}

		return angles;
	}

	// HELPERS ---------------------------------------------------------------------------------------------------------------

	float PolyRadiusFromSidesCount(float otherAngle) {
		return 1/(2 * Mathf.Sin(otherAngle * Mathf.Deg2Rad/2));
	}

	float DistanceFromCenterForSideWithArcAngle(float arcAngle) {
		return 1/(2 * Mathf.Tan(arcAngle * Mathf.Deg2Rad/2));
	}

	string ArrayToString (float[] array) {
		var s = "";
		for (var i = 0; i < array.Length; i++) {
			s += array[i].ToString("0");
			s += ",";
		}

		return s;
	}
}